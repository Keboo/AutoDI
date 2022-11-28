using System.Reflection;
using System.Runtime.CompilerServices;

using AutoDI.Build.CodeGen;

using Mono.Cecil;
using Mono.Cecil.Rocks;

using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using Instruction = Mono.Cecil.Cil.Instruction;
using OpCodes = Mono.Cecil.Cil.OpCodes;

[assembly: InternalsVisibleTo("AutoDI.Build.Tests")]
[assembly: InternalsVisibleTo("AutoDI.Generator")]

namespace AutoDI.Build;

public partial class ProcessAssemblyTask : AssemblyRewriteTask
{
    protected override bool WeaveAssembly(AssemblyRewiteTaskContext context)
    {
        try
        {
            context.Debug($"Starting AutoDI Weaver v{GetType().Assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version}", DebugLogLevel.Default);

            var typeResolver = new TypeResolver(context);

            Settings? settings = LoadSettings(context);
            if (settings is null) return false;

            ICollection<TypeDefinition> allTypes = typeResolver.GetAllTypes(settings);

            context.Debug($"Found types:{Environment.NewLine}{string.Join(Environment.NewLine, allTypes.Select(x => x.FullName))}", DebugLogLevel.Verbose);

            if (context.ResolveAssembly($"AutoDI, Version={Assembly.GetExecutingAssembly().GetName().Version}, Culture=neutral, PublicKeyToken=null") is null)
            {
                context.Error("Could not find AutoDI assembly. Ensure the project references AutoDI.", null);
                return false;
            }

            LoadRequiredData(context);

            ICodeGenerator? gen = GetCodeGenerator(settings, context);

            if (settings.GenerateRegistrations)
            {
                Mapping mapping = Mapping.GetMapping(settings, allTypes, context);

                context.Debug($"Found potential map:{Environment.NewLine}{mapping}", DebugLogLevel.Verbose);

                context.ModuleDefinition.Types.Add(GenerateAutoDIClass(context.ModuleDefinition, mapping, settings, gen, context, out MethodDefinition initMethod));

                switch (settings.InitMode)
                {
                    case InitMode.Manual:
                        context.Debug("Skipping injections of Init method", DebugLogLevel.Verbose);
                        break;
                    case InitMode.EntryPoint:
                        InjectInitCall(context.ModuleDefinition, initMethod, context);
                        break;
                    case InitMode.ModuleLoad:
                        InjectModuleCctorInitCall(context.ModuleDefinition, initMethod, context);
                        break;
                    default:
                        context.Warning($"Unsupported InitMode: {settings.InitMode}");
                        break;
                }
            }
            else
            {
                context.Debug("Skipping registration", DebugLogLevel.Verbose);
            }

            //We only update types in our module
            foreach (TypeDefinition type in allTypes.Where(type => type.Module == context.ModuleDefinition))
            {
                ProcessType(context, type, gen);
            }
            gen?.Save();
            return true;
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
                sb.AppendLine(e.ToString());
            context.Error(sb.ToString(), null);
            return false;
        }
    }

    private void ProcessType(AssemblyRewiteTaskContext context, TypeDefinition type, ICodeGenerator? generator)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            ProcessMethod(context, type, method, generator);
        }
    }

    private static ICodeGenerator? GetCodeGenerator(Settings settings, AssemblyRewiteTaskContext context)
    {
        switch (settings.DebugCodeGeneration)
        {
            case CodeLanguage.CSharp:
                var genDir = Path.Combine(Path.GetDirectoryName(context.ModuleDefinition.FileName), "AutoDI.Generated");
                context.Debug($"Generating temp file in '{genDir}'", DebugLogLevel.Verbose);
                return new CSharpCodeGenerator(genDir);
            default:
                return null;
        }
    }

    private void ProcessMethod(AssemblyRewiteTaskContext context,
        TypeDefinition type, MethodDefinition method, ICodeGenerator? generator)
    {
        List<ParameterDefinition> dependencyParameters = method.Parameters.Where(
            p => p.CustomAttributes.Any(a => a.AttributeType.IsType(Import.AutoDI.DependencyAttributeType))).ToList();

        List<PropertyDefinition> dependencyProperties = method.IsConstructor ?
            type.Properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType.IsType(Import.AutoDI.DependencyAttributeType))).ToList() :
            new List<PropertyDefinition>();

        if (dependencyParameters.Any() || dependencyProperties.Any())
        {
            context.Debug($"Processing method '{method.Name}' for '{method.DeclaringType.FullName}'", DebugLogLevel.Verbose);

            var injector = new Injector(method);

            IMethodGenerator? methodGenerator = generator?.Method(method);
            foreach (ParameterDefinition parameter in dependencyParameters)
            {
                if (!parameter.IsOptional)
                {
                    context.Info(
                        $"Constructor parameter {parameter.ParameterType.Name} {parameter.Name} is marked with {Import.AutoDI.DependencyAttributeType.FullName} but is not an optional parameter. In {type.FullName}.");
                }
                if (parameter.Constant != null)
                {
                    context.Warning(
                        $"Constructor parameter {parameter.ParameterType.Name} {parameter.Name} in {type.FullName} does not have a null default value. AutoDI will only resolve dependencies that are null");
                }

                var initInstruction = Instruction.Create(OpCodes.Ldarg, parameter);
                var storeInstruction = Instruction.Create(OpCodes.Starg, parameter);
                ResolveDependency(
                    parameter.ParameterType,
                    parameter,
                    new[] { initInstruction },
                    null,
                    storeInstruction,
                    parameter.Name);
            }


            foreach (PropertyDefinition property in dependencyProperties)
            {
                //NB: Constant string, compiler detail... yuck yuck and double yuck
                FieldDefinition? backingField = property.DeclaringType.Fields.FirstOrDefault(f => f.Name == $"<{property.Name}>k__BackingField");
                //Store the return from the resolve method in the method parameter
                if (property.SetMethod is null && backingField is null)
                {
                    context.Warning(
                        $"{property.FullName} is marked with {Import.AutoDI.DependencyAttributeType.FullName} but cannot be set. Dependency properties must either be auto properties or have a setter");
                    continue;
                }

                ResolveDependency(property.PropertyType, property,
                    new[]
                    {
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, property.GetMethod),
                    },
                    Instruction.Create(OpCodes.Ldarg_0),
                    backingField is not null
                        ? Instruction.Create(OpCodes.Stfld, backingField)
                        : Instruction.Create(OpCodes.Call, property.SetMethod),
                    property.Name);
                
                //remove auto property null initializers
                if (backingField is not null)
                {
                    RemoveAutoPropertyInitializer(backingField);
                }
            }

            methodGenerator?.Append($"//We now return you to your regularly scheduled method{Environment.NewLine}");

            method.Body.OptimizeMacros();

            void RemoveAutoPropertyInitializer(FieldDefinition backingField)
            {
                var instructions = method.Body.Instructions;
                for (int i = 0; i < instructions.Count; i++)
                {
                    if (instructions[i].OpCode == OpCodes.Ldarg_0 &&
                        instructions[i + 1].OpCode == OpCodes.Ldnull &&
                        instructions[i + 2].OpCode == OpCodes.Stfld &&
                        instructions[i + 2].Operand == backingField)
                    {
                        //Ldarg0
                        instructions.RemoveAt(i);
                        //Ldnull
                        instructions.RemoveAt(i);
                        //Stfld
                        instructions.RemoveAt(i);
                        break;
                    }
                }
            }
            
            void ResolveDependency(
                TypeReference dependencyType,
                ICustomAttributeProvider source,
                Instruction[] loadSource,
                Instruction? resolveAssignmentTarget,
                Instruction setResult,
                string dependencyName)
            {
                //Push dependency parameter onto the stack
                if (methodGenerator != null)
                {
                    methodGenerator.Append($"if (ReferenceEquals({dependencyName}, null))", loadSource.First());
                    methodGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
                }

                injector.Insert(loadSource);
                var afterParam = Instruction.Create(OpCodes.Nop);
                //Push null onto the stack
                injector.Insert(OpCodes.Ldnull);
                //Push 1 if the values are equal, 0 if they are not equal
                injector.Insert(OpCodes.Ceq);
                //Branch if the value is false (0), the dependency was set by the caller we wont replace it
                injector.Insert(OpCodes.Brfalse_S, afterParam);
                //Push the dependency resolver onto the stack
                if (resolveAssignmentTarget != null)
                {
                    injector.Insert(resolveAssignmentTarget);
                }

                //Create parameters array
                var dependencyAttribute = source.CustomAttributes.First(x => x.AttributeType.IsType(Import.AutoDI.DependencyAttributeType));
                var values =
                    (dependencyAttribute.ConstructorArguments?.FirstOrDefault().Value as CustomAttributeArgument[])
                    ?.Select(x => x.Value)
                    .OfType<CustomAttributeArgument>()
                    .ToArray();
                //Create array of appropriate length
                Instruction loadArraySize = injector.Insert(OpCodes.Ldc_I4, values?.Length ?? 0);

                if (methodGenerator != null)
                {
                    methodGenerator.Append($"    {dependencyName} = GlobalDI.GetService<{dependencyType.FullNameCSharp()}>();", resolveAssignmentTarget ?? loadArraySize);
                    methodGenerator.Append(Environment.NewLine);
                }

                injector.Insert(OpCodes.Newarr, context.ModuleDefinition.ImportReference(Import.System.Object));
                if (values?.Length > 0)
                {
                    for (int i = 0; i < values.Length; ++i)
                    {
                        injector.Insert(OpCodes.Dup);
                        //Push the array index to insert
                        injector.Insert(OpCodes.Ldc_I4, i);
                        //Insert constant value with any boxing/conversion needed
                        ProcessAssemblyTask.InsertObjectConstant(context, injector, values[i].Value, values[i].Type.Resolve());
                        //Push the object into the array at index
                        injector.Insert(OpCodes.Stelem_Ref);
                    }
                }

                //Call the resolve method
                var getServiceMethod = new GenericInstanceMethod(Import.AutoDI.GlobalDI.GetService)
                {
                    GenericArguments = { context.ModuleDefinition.ImportReference(dependencyType) }
                };
                injector.Insert(OpCodes.Call, getServiceMethod);
                //Set the return from the resolve method into the parameter
                injector.Insert(setResult);
                injector.Insert(afterParam);

                if (methodGenerator != null)
                {
                    methodGenerator.Append("}", afterParam);
                    methodGenerator.Append(Environment.NewLine);
                }
            }
        }
    }

    private static void InsertObjectConstant(AssemblyRewiteTaskContext context, Injector injector, object constant, TypeDefinition type)
    {
        if (constant is null)
        {
            injector.Insert(OpCodes.Ldnull);
            return;
        }
        if (type.IsEnum)
        {
            InsertAndBoxConstant(context, injector, constant, type.GetEnumUnderlyingType(), type);
        }
        else
        {
            var typeDef = context.ModuleDefinition.ImportReference(constant.GetType());
            InsertAndBoxConstant(context, injector, constant, typeDef, constant is string ? null : typeDef);
        }
    }

    private static void InsertAndBoxConstant(
        AssemblyRewiteTaskContext context,
        Injector injector,
        object constant,
        TypeReference type,
        TypeReference? boxType = null)
    {
        if (type.IsType<string>())
        {
            injector.Insert(OpCodes.Ldstr, (string)constant);
        }
        else if (type.IsType<int>())
        {
            injector.Insert(OpCodes.Ldc_I4, (int)constant);
        }
        else if (type.IsType<long>())
        {
            injector.Insert(OpCodes.Ldc_I8, (long)constant);
        }
        else if (type.IsType<double>())
        {
            injector.Insert(OpCodes.Ldc_R8, (double)constant);
        }
        else if (type.IsType<float>())
        {
            injector.Insert(OpCodes.Ldc_R4, (float)constant);
        }
        else if (type.IsType<short>())
        {
            injector.Insert(OpCodes.Ldc_I4, (short)constant);
        }
        else if (type.IsType<byte>())
        {
            injector.Insert(OpCodes.Ldc_I4, (byte)constant);
        }
        else if (type.IsType<uint>())
        {
            injector.Insert(OpCodes.Ldc_I4, (int)(uint)constant);
        }
        else if (type.IsType<ulong>())
        {
            injector.Insert(OpCodes.Ldc_I8, (long)(ulong)constant);
        }
        else if (type.IsType<ushort>())
        {
            injector.Insert(OpCodes.Ldc_I4, (ushort)constant);
        }
        else if (type.IsType<sbyte>())
        {
            injector.Insert(OpCodes.Ldc_I4, (sbyte)constant);
        }
        else
        {
            context.Warning($"Unknown constant type {type.FullName}");
        }
        if (boxType != null)
        {
            injector.Insert(Instruction.Create(OpCodes.Box, boxType));
        }
    }

    protected override IEnumerable<string> GetAssembliesToInclude()
    {
        return base.GetAssembliesToInclude().Concat(GetAssembliesToInclude());

        static IEnumerable<string> GetAssembliesToInclude()
        {
            yield return "AutoDI";
            yield return "Microsoft.Extensions.DependencyInjection.Abstractions";
        }
    }
}