using AutoDI.Fody;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using OpCodes = Mono.Cecil.Cil.OpCodes;

[assembly: InternalsVisibleTo("AutoDI.Fody.Tests")]
[assembly: InternalsVisibleTo("AutoDI.Generator")]
// ReSharper disable once CheckNamespace
public partial class ModuleWeaver : BaseModuleWeaver
{
    private Action<string, AutoDI.DebugLogLevel> InternalLogDebug { get; set; }

    public override void Execute()
    {
        InternalLogDebug = (s, l) => LogDebug(s);
        Logger = new WeaverLogger(this);
        
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

            Logger.Debug($"Starting AutoDI Weaver v{GetType().Assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version}", AutoDI.DebugLogLevel.Default);

            var typeResolver = new TypeResolver(ModuleDefinition, ModuleDefinition.AssemblyResolver, Logger);

            var di = ResolveAssembly("Microsoft.Extensions.DependencyInjection.Abstractions");
            Logger.Debug($"Found DI: {di?.FullName}", AutoDI.DebugLogLevel.Default);

            Settings settings = LoadSettings(typeResolver);
            if (settings == null) return;

            ICollection<TypeDefinition> allTypes = typeResolver.GetAllTypes(settings, out AssemblyDefinition autoDIAssembly);

            Logger.Debug($"Found types:\r\n{string.Join("\r\n", allTypes.Select(x => x.FullName))}", AutoDI.DebugLogLevel.Verbose);

            if (autoDIAssembly == null)
            {
                autoDIAssembly = ResolveAssembly("AutoDI");
                if (autoDIAssembly == null)
                {
                    Logger.Warning("Could not find AutoDI assembly");
                    return;
                }
                else
                {
                    Logger.Warning($"Failed to find AutoDI assembly. Manually injecting '{autoDIAssembly.MainModule.FileName}'");
                }
            }

            LoadRequiredData();

            Logger.Debug($"Found IServiceCollection: {Import.IServiceCollection.DeclaringType.Module.Assembly.FullName}", AutoDI.DebugLogLevel.Default);

            if (settings.GenerateRegistrations)
            {
                Mapping mapping = Mapping.GetMapping(settings, allTypes, Logger);

                Logger.Debug($"Found potential map:\r\n{mapping}", AutoDI.DebugLogLevel.Verbose);

                ModuleDefinition.Types.Add(GenerateAutoDIClass(mapping, settings, out MethodDefinition initMethod));

                if (settings.AutoInit)
                {
                    InjectInitCall(initMethod);
                }
            }
            else
            {
                Logger.Debug("Skipping registration", AutoDI.DebugLogLevel.Verbose);
            }

            //We only update types in our module
            foreach (TypeDefinition type in allTypes.Where(type => type.Module == ModuleDefinition))
            {
                ProcessType(type);
            }
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
                sb.AppendLine(e.ToString());
            Logger.Error(sb.ToString());
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainOnAssemblyResolve;
        }
    }

    private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        var assembly = ResolveAssembly(args.Name);
        if (assembly == null)
        {
            Logger.Warning($"Failed to resolve assembly '{args.Name}'");
            return null;
        }
        Logger.Debug($"Resolved assembly '{assembly.FullName}'", AutoDI.DebugLogLevel.Verbose);
        using (var memoryStream = new MemoryStream())
        {
            assembly.Write(memoryStream);
            memoryStream.Position = 0;
            return Assembly.Load(memoryStream.ToArray());
        }
    }

    private void ProcessType(TypeDefinition type)
    {
        foreach (MethodDefinition ctor in type.Methods.Where(x => x.IsConstructor))
        {
            Logger.Debug($"Processing constructor for '{ctor.DeclaringType.FullName}'", AutoDI.DebugLogLevel.Verbose);
            ProcessConstructor(type, ctor);
        }
    }

    private void ProcessConstructor(TypeDefinition type, MethodDefinition constructor)
    {
        List<ParameterDefinition> dependencyParameters = constructor.Parameters.Where(
                        p => p.CustomAttributes.Any(a => a.AttributeType.IsType(Import.AutoDI.DependencyAttributeType))).ToList();

        List<PropertyDefinition> dependencyProperties = type.Properties.Where(
            p => p.CustomAttributes.Any(a => a.AttributeType.IsType(Import.AutoDI.DependencyAttributeType))).ToList();

        if (dependencyParameters.Any() || dependencyProperties.Any())
        {
            var injector = new Injector(constructor);

            var end = Instruction.Create(OpCodes.Nop);

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

                ResolveDependency(parameter.ParameterType, parameter,
                    new[] { Instruction.Create(OpCodes.Ldarg, parameter) },
                    null,
                    Instruction.Create(OpCodes.Starg, parameter));
            }

            foreach (PropertyDefinition property in dependencyProperties)
            {
                FieldDefinition backingField = null;
                //Store the return from the resolve method in the method parameter
                if (property.SetMethod == null)
                {
                    //NB: Constant string, compiler detail... yuck yuck and double duck
                    backingField = property.DeclaringType.Fields.FirstOrDefault(f => f.Name == $"<{property.Name}>k__BackingField");
                    if (backingField == null)
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
                        : Instruction.Create(OpCodes.Stfld, backingField));
            }

            injector.Insert(end);

            constructor.Body.OptimizeMacros();

            void ResolveDependency(TypeReference dependencyType, ICustomAttributeProvider source,
                IEnumerable<Instruction> loadSource,
                Instruction resolveAssignmentTarget,
                Instruction setResult)
            {
                //Push dependency parameter onto the stack
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
                injector.Insert(OpCodes.Ldc_I4, values?.Length ?? 0);
                injector.Insert(OpCodes.Newarr, ModuleDefinition.ImportReference(typeof(object)));
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
            }
        }
    }

    private void InsertObjectConstant(Injector injector, object constant, TypeDefinition type)
    {
        if (ReferenceEquals(constant, null))
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

    private void InsertAndBoxConstant(Injector injector, object constant, TypeReference type, TypeReference boxType = null)
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
        if (boxType != null)
        {
            injector.Insert(Instruction.Create(OpCodes.Box, boxType));
        }
        Logger.Warning($"Unknown constant type {constant.GetType().FullName}");
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
        yield return "System.Runtime";
        yield return "System.Core";
        yield return "netstandard";
        yield return "AutoDI";
        yield return "Microsoft.Extensions.DependencyInjection.Abstractions";
        yield return "System.Collections";
        yield return "System.ObjectModel";
        yield return "System.Threading";
    }
}

