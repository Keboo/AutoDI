using AutoDI.Build.CodeGen;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System.Reflection;
using System.Runtime.CompilerServices;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using Instruction = Mono.Cecil.Cil.Instruction;
using OpCodes = Mono.Cecil.Cil.OpCodes;

[assembly: InternalsVisibleTo("AutoDI.Build.Tests")]
[assembly: InternalsVisibleTo("AutoDI.Generator")]

namespace AutoDI.Build;

public partial class ProcessAssemblyTask : AssemblyRewriteTask
{
    protected override bool WeaveAssembly()
    {
        try
        {
            Logger.Debug($"Starting AutoDI Weaver v{GetType().Assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version}", DebugLogLevel.Default);

            var typeResolver = new TypeResolver(ModuleDefinition, ModuleDefinition.AssemblyResolver, Logger);

            Settings settings = LoadSettings();
            if (settings is null) return false;

            ICollection<TypeDefinition> allTypes = typeResolver.GetAllTypes(settings);

            Logger.Debug($"Found types:\r\n{string.Join("\r\n", allTypes.Select(x => x.FullName))}", DebugLogLevel.Verbose);

            if (ResolveAssembly($"AutoDI, Version={Assembly.GetExecutingAssembly().GetName().Version}, Culture=neutral, PublicKeyToken=null") is null)
            {
                Logger.Error("Could not find AutoDI assembly. Ensure the project references AutoDI.", null);
                return false;
            }

            LoadRequiredData();

            ICodeGenerator? gen = GetCodeGenerator(settings);

            if (settings.GenerateRegistrations)
            {
                Mapping mapping = Mapping.GetMapping(settings, allTypes, Logger);

                Logger.Debug($"Found potential map:\r\n{mapping}", DebugLogLevel.Verbose);

                ModuleDefinition.Types.Add(GenerateAutoDIClass(mapping, settings, gen, out MethodDefinition initMethod));

                switch (settings.InitMode)
                {
                    case InitMode.Manual:
                        Logger.Debug("Skipping injections of Init method", DebugLogLevel.Verbose);
                        break;
                    case InitMode.EntryPoint:
                        InjectInitCall(initMethod);
                        break;
                    case InitMode.ModuleLoad:
                        InjectModuleCctorInitCall(initMethod);
                        break;
                    default:
                        Logger.Warning($"Unsupported InitMode: {settings.InitMode}");
                        break;
                }
            }
            else
            {
                Logger.Debug("Skipping registration", DebugLogLevel.Verbose);
            }

            //We only update types in our module
            foreach (TypeDefinition type in allTypes.Where(type => type.Module == ModuleDefinition))
            {
                ProcessType(type, gen);
            }
            gen?.Save();
            return true;
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
                sb.AppendLine(e.ToString());
            Logger.Error(sb.ToString(), null);
            return false;
        }
    }

    private void ProcessType(TypeDefinition type, ICodeGenerator generator)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            ProcessMethod(type, method, generator);
        }
    }

    private ICodeGenerator? GetCodeGenerator(Settings settings)
    {
        switch (settings.DebugCodeGeneration)
        {
            case CodeLanguage.CSharp:
                var genDir = Path.Combine(Path.GetDirectoryName(ModuleDefinition.FileName), "AutoDI.Generated");
                Logger.Debug($"Generating temp file in '{genDir}'", DebugLogLevel.Verbose);
                return new CSharpCodeGenerator(genDir);
            default:
                return null;
        }
    }

    private void ProcessMethod(TypeDefinition type, MethodDefinition method, ICodeGenerator generator)
    {
        List<ParameterDefinition> dependencyParameters = method.Parameters.Where(
            p => p.CustomAttributes.Any(a => a.AttributeType.IsType(Import.AutoDI.DependencyAttributeType))).ToList();

        List<PropertyDefinition> dependencyProperties = method.IsConstructor ?
            type.Properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType.IsType(Import.AutoDI.DependencyAttributeType))).ToList() :
            new List<PropertyDefinition>();

        if (dependencyParameters.Any() || dependencyProperties.Any())
        {
            Logger.Debug($"Processing method '{method.Name}' for '{method.DeclaringType.FullName}'", DebugLogLevel.Verbose);

            var injector = new Injector(method);

            IMethodGenerator? methodGenerator = generator?.Method(method);
            foreach (ParameterDefinition parameter in dependencyParameters)
            {
                if (!parameter.IsOptional)
                {
                    Logger.Info(
                        $"Constructor parameter {parameter.ParameterType.Name} {parameter.Name} is marked with {Import.AutoDI.DependencyAttributeType.FullName} but is not an optional parameter. In {type.FullName}.");
                }
                if (parameter.Constant != null)
                {
                    Logger.Warning(
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
                FieldDefinition backingField = null;
                //Store the return from the resolve method in the method parameter
                if (property.SetMethod is null)
                {
                    //NB: Constant string, compiler detail... yuck yuck and double duck
                    backingField = property.DeclaringType.Fields.FirstOrDefault(f => f.Name == $"<{property.Name}>k__BackingField");
                    if (backingField is null)
                    {
                        Logger.Warning(
                            $"{property.FullName} is marked with {Import.AutoDI.DependencyAttributeType.FullName} but cannot be set. Dependency properties must either be auto properties or have a setter");
                        continue;
                    }
                }

                //injector.Insert(OpCodes.Call, property.GetMethod);
                ResolveDependency(property.PropertyType, property,
                    new[]
                    {
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, property.GetMethod),
                    },
                    Instruction.Create(OpCodes.Ldarg_0),
                    property.SetMethod != null
                        ? Instruction.Create(OpCodes.Call, property.SetMethod)
                        : Instruction.Create(OpCodes.Stfld, backingField),
                    property.Name);
            }

            methodGenerator?.Append($"//We now return you to your regularly scheduled method{Environment.NewLine}");

            method.Body.OptimizeMacros();

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
                    methodGenerator.Append($"if ({dependencyName} == null)", loadSource.First());
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

                injector.Insert(OpCodes.Newarr, ModuleDefinition.ImportReference(Import.System.Object));
                if (values?.Length > 0)
                {
                    for (int i = 0; i < values.Length; ++i)
                    {
                        injector.Insert(OpCodes.Dup);
                        //Push the array index to insert
                        injector.Insert(OpCodes.Ldc_I4, i);
                        //Insert constant value with any boxing/conversion needed
                        InsertObjectConstant(injector, values[i].Value, values[i].Type.Resolve());
                        //Push the object into the array at index
                        injector.Insert(OpCodes.Stelem_Ref);
                    }
                }

                //Call the resolve method
                var getServiceMethod = new GenericInstanceMethod(Import.AutoDI.GlobalDI.GetService)
                {
                    GenericArguments = { ModuleDefinition.ImportReference(dependencyType) }
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

    private void InsertObjectConstant(Injector injector, object constant, TypeDefinition type)
    {
        if (constant is null)
        {
            injector.Insert(OpCodes.Ldnull);
            return;
        }
        if (type.IsEnum)
        {
            InsertAndBoxConstant(injector, constant, type.GetEnumUnderlyingType(), type);
        }
        else
        {
            var typeDef = ModuleDefinition.ImportReference(constant.GetType());
            InsertAndBoxConstant(injector, constant, typeDef, constant is string ? null : typeDef);
        }
    }

    private void InsertAndBoxConstant(
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
            Logger.Warning($"Unknown constant type {type.FullName}");
        }
        if (boxType != null)
        {
            injector.Insert(Instruction.Create(OpCodes.Box, boxType));
        }
    }

    protected override IEnumerable<string> GetAssembliesToInclude()
    {
        return base.GetAssembliesToInclude().Concat(GetAssembliesToInclude());

        IEnumerable<string> GetAssembliesToInclude()
        {
            yield return "AutoDI";
            yield return "Microsoft.Extensions.DependencyInjection.Abstractions";
        }
    }
}