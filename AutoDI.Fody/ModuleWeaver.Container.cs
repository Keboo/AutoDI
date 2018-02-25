
using System;
using System.Linq;
using AutoDI;
using AutoDI.Fody;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

// ReSharper disable once CheckNamespace
partial class ModuleWeaver
{
    //TODO: out parameters... yuck
    private TypeDefinition GenerateAutoDIClass(Mapping mapping,
        out MethodDefinition initMethod)
    {
        var containerType = new TypeDefinition(DI.Namespace, DI.TypeName,
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed
            | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit)
        {
            BaseType = ModuleDefinition.Get<object>()
        };

        FieldDefinition globalServiceProvider =
            ModuleDefinition.CreateStaticReadonlyField(DI.GlobalServiceProviderName, false, Import.IServiceProvider);
        containerType.Fields.Add(globalServiceProvider);

        MethodDefinition configureMethod = GenerateConfigureMethod(mapping, containerType);
        containerType.Methods.Add(configureMethod);

        initMethod = GenerateInitMethod(configureMethod, globalServiceProvider);
        containerType.Methods.Add(initMethod);

        MethodDefinition disposeMethod = GenerateDisposeMethod(globalServiceProvider);
        containerType.Methods.Add(disposeMethod);

        return containerType;
    }

    private MethodDefinition GenerateConfigureMethod(Mapping mapping, TypeDefinition containerType)
    {
        var method = new MethodDefinition(nameof(DI.AddServices),
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            ModuleDefinition.ImportReference(typeof(void)));

        var serviceCollection = new ParameterDefinition("collection", ParameterAttributes.None, ModuleDefinition.Get<IServiceCollection>());
        method.Parameters.Add(serviceCollection);

        ILProcessor processor = method.Body.GetILProcessor();

        MethodDefinition funcCtor = ModuleDefinition.ResolveCoreConstructor(typeof(Func<,>));

        if (mapping != null)
        {
            int factoryIndex = 0;
            foreach (TypeMap map in mapping)
            {
                try
                {
                    InternalLogDebug($"Processing map for {map.TargetType.FullName}", DebugLogLevel.Verbose);

                    MethodDefinition factoryMethod = GenerateFactoryMethod(map.TargetType, factoryIndex);
                    if (factoryMethod == null)
                    {
                        InternalLogDebug($"No acceptable constructor for '{map.TargetType.FullName}', skipping map",
                            DebugLogLevel.Verbose);
                        continue;
                    }
                    containerType.Methods.Add(factoryMethod);
                    factoryIndex++;

                    foreach (TypeLifetime typeLifetime in map.Lifetimes)
                    {
                        processor.Emit(OpCodes.Ldarg_0); //collection parameter

                        processor.Emit(OpCodes.Ldnull);
                        processor.Emit(OpCodes.Ldftn, factoryMethod);
                        processor.Emit(OpCodes.Newobj,
                            ModuleDefinition.ImportReference(
                                funcCtor.MakeGenericTypeConstructor(Import.IServiceProvider,
                                    map.TargetType)));

                        processor.Emit(OpCodes.Ldc_I4, typeLifetime.Keys.Count);
                        processor.Emit(OpCodes.Newarr, Import.System_Type);

                        int arrayIndex = 0;

                        foreach (TypeDefinition key in typeLifetime.Keys)
                        {
                            TypeReference importedKey = ModuleDefinition.ImportReference(key);
                            InternalLogDebug(
                                $"Mapping {importedKey.FullName} => {map.TargetType.FullName} ({typeLifetime.Lifetime})",
                                DebugLogLevel.Default);
                            processor.Emit(OpCodes.Dup);
                            processor.Emit(OpCodes.Ldc_I4, arrayIndex++);
                            processor.Emit(OpCodes.Ldtoken, importedKey);
                            processor.Emit(OpCodes.Call, Import.Type_GetTypeFromHandle);
                            processor.Emit(OpCodes.Stelem_Ref);
                        }

                        processor.Emit(OpCodes.Ldc_I4, (int)typeLifetime.Lifetime);

                        var genericAddMethod =
                            new GenericInstanceMethod(Import.ServiceCollectionMixins_AddAutoDIService);
                        genericAddMethod.GenericArguments.Add(ModuleDefinition.ImportReference(map.TargetType));
                        processor.Emit(OpCodes.Call, genericAddMethod);
                        processor.Emit(OpCodes.Pop);
                    }
                }
                catch (MultipleConstructorException e)
                {
                    LogError($"Failed to create map for {map}\r\n{e}");
                }
                catch (Exception e)
                {
                    LogWarning($"Failed to create map for {map}\r\n{e}");
                }
            }
        }

        processor.Emit(OpCodes.Ret);

        return method;
    }

    private MethodDefinition GenerateFactoryMethod(TypeDefinition targetType, int index)
    {
        if (!targetType.CanMapType()) return null;
        
        MethodDefinition targetTypeCtor = targetType.GetMappingConstructor();
        if (targetTypeCtor == null) return null;

        var factory = new MethodDefinition($"<{targetType.Name}>_generated_{index}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            ModuleDefinition.ImportReference(targetType));
        factory.Parameters.Add(new ParameterDefinition("serviceProvider", ParameterAttributes.None, Import.IServiceProvider));

        ILProcessor factoryProcessor = factory.Body.GetILProcessor();

        MethodReference getServiceMethod = ModuleDefinition.GetMethod(typeof(ServiceProviderServiceExtensions),
            nameof(ServiceProviderServiceExtensions.GetService));

        foreach (ParameterDefinition parameter in targetTypeCtor.Parameters)
        {
            factoryProcessor.Emit(OpCodes.Ldarg_0);
            var genericGetService = new GenericInstanceMethod(getServiceMethod);
            genericGetService.GenericArguments.Add(ModuleDefinition.ImportReference(parameter.ParameterType));
            factoryProcessor.Emit(OpCodes.Call, genericGetService);
        }

        factoryProcessor.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(targetTypeCtor));
        factoryProcessor.Emit(OpCodes.Ret);
        return factory;
    }

    private MethodDefinition GenerateInitMethod(MethodDefinition configureMethod, FieldDefinition globalServiceProvider)
    {
        var initMethod = new MethodDefinition(nameof(DI.Init),
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            ModuleDefinition.ImportReference(typeof(void)));
        var configureAction = new ParameterDefinition("configure", ParameterAttributes.None, ModuleDefinition.Get<Action<IApplicationBuilder>>());
        initMethod.Parameters.Add(configureAction);

        var applicationBuilder = new VariableDefinition(ModuleDefinition.Get<IApplicationBuilder>());
        initMethod.Body.Variables.Add(applicationBuilder);
        ILProcessor initProcessor = initMethod.Body.GetILProcessor();

        Instruction createApplicationbuilder = Instruction.Create(OpCodes.Newobj, ModuleDefinition.GetDefaultConstructor<ApplicationBuilder>());

        initProcessor.Emit(OpCodes.Ldsfld, globalServiceProvider);
        initProcessor.Emit(OpCodes.Brfalse_S, createApplicationbuilder);
        //Compare
        initProcessor.Emit(OpCodes.Newobj, ModuleDefinition.GetConstructor<AlreadyInitializedException>());
        initProcessor.Emit(OpCodes.Throw);

        initProcessor.Append(createApplicationbuilder);
        initProcessor.Emit(OpCodes.Stloc_0);

        initProcessor.Emit(OpCodes.Ldloc_0); //applicationBuilder
        initProcessor.Emit(OpCodes.Ldnull);
        initProcessor.Emit(OpCodes.Ldftn, configureMethod);
        initProcessor.Emit(OpCodes.Newobj, ModuleDefinition.GetConstructor<Action<IServiceCollection>>());
        initProcessor.Emit(OpCodes.Callvirt, ModuleDefinition.GetMethod<IApplicationBuilder>(nameof(IApplicationBuilder.ConfigureServices)));
        initProcessor.Emit(OpCodes.Pop);

        MethodDefinition setupMethod = FindSetupMethod();
        if (setupMethod != null)
        {
            InternalLogDebug($"Found setup method '{setupMethod.FullName}'", DebugLogLevel.Default);
            initProcessor.Emit(OpCodes.Ldloc_0); //applicationBuilder
            initProcessor.Emit(OpCodes.Call, setupMethod);
            initProcessor.Emit(OpCodes.Nop);
        }
        else
        {
            InternalLogDebug("No setup method found", DebugLogLevel.Default);
        }

        Instruction loadForBuild = Instruction.Create(OpCodes.Ldloc_0);

        initProcessor.Emit(OpCodes.Ldarg_0);
        initProcessor.Emit(OpCodes.Brfalse_S, loadForBuild);
        initProcessor.Emit(OpCodes.Ldarg_0);
        initProcessor.Emit(OpCodes.Ldloc_0);
        initProcessor.Emit(OpCodes.Callvirt, ModuleDefinition.GetMethod<Action<IApplicationBuilder>>(nameof(Action<IApplicationBuilder>.Invoke)));


        initProcessor.Append(loadForBuild);
        var buildMethod = ModuleDefinition.GetMethod<IApplicationBuilder>(nameof(IApplicationBuilder.Build));
        buildMethod.ReturnType = Import.IServiceProvider; //Must update the return type to handle .net core apps
        initProcessor.Emit(OpCodes.Callvirt, buildMethod);
        initProcessor.Emit(OpCodes.Stsfld, globalServiceProvider);

        initProcessor.Emit(OpCodes.Ldsfld, globalServiceProvider);
        initProcessor.Emit(OpCodes.Call, Import.GlobalDI_Register);


        initProcessor.Emit(OpCodes.Ret);

        return initMethod;
    }

    private MethodDefinition GenerateDisposeMethod(FieldDefinition globalServiceProvider)
    {
        var disposeMethod = new MethodDefinition(nameof(DI.Dispose),
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            ModuleDefinition.ImportReference(typeof(void)));

        VariableDefinition disposable = new VariableDefinition(ModuleDefinition.Get<IDisposable>());
        disposeMethod.Body.Variables.Add(disposable);

        ILProcessor processor = disposeMethod.Body.GetILProcessor();
        Instruction afterDispose = Instruction.Create(OpCodes.Nop);

        processor.Emit(OpCodes.Ldsfld, globalServiceProvider);
        processor.Emit(OpCodes.Isinst, ModuleDefinition.Get<IDisposable>());
        processor.Emit(OpCodes.Dup);
        processor.Emit(OpCodes.Stloc_0); //disposable
        processor.Emit(OpCodes.Brfalse_S, afterDispose);
        processor.Emit(OpCodes.Ldloc_0); //disposable
        processor.Emit(OpCodes.Callvirt, ModuleDefinition.GetMethod<IDisposable>(nameof(IDisposable.Dispose)));

        processor.Append(afterDispose);

        processor.Emit(OpCodes.Ldsfld, globalServiceProvider);
        processor.Emit(OpCodes.Call, Import.GlobalDI_Unregister);
        processor.Emit(OpCodes.Pop);

        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Stsfld, globalServiceProvider);

        processor.Emit(OpCodes.Ret);
        return disposeMethod;
    }

    private MethodDefinition FindSetupMethod()
    {
        foreach (var method in ModuleDefinition.GetAllTypes().SelectMany(t => t.GetMethods())
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.IsType<SetupMethodAttribute>())))
        {
            if (!method.IsStatic)
            {
                LogWarning($"Setup method '{method.FullName}' must be static");
                return null;
            }
            if (!method.IsPublic && !method.IsAssembly)
            {
                LogWarning($"Setup method '{method.FullName}' must be public or internal");
                return null;
            }
            if (method.Parameters.Count != 1 || !method.Parameters[0].ParameterType.IsType<IApplicationBuilder>())
            {
                LogWarning($"Setup method '{method.FullName}' must take a single parameter of type '{typeof(IApplicationBuilder).FullName}'");
                return null;
            }
            return method;
        }
        return null;
    }
}