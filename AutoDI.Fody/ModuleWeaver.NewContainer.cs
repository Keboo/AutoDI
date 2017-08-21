
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
    //TODO: Better name
    //TODO: out parameter... yuck
    private TypeDefinition GenerateContainer(Mapping mapping, out MethodDefinition getGlobalServiceProvider)
    {
        var containerType = new TypeDefinition(DI.Namespace, DI.TypeName,
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed
            | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit)
        {
            BaseType = ModuleDefinition.Get<object>()
        };

        FieldDefinition globalServiceProvider =
            ModuleDefinition.CreateStaticReadonlyField<IServiceProvider>("_globalServiceProvider", false);
        containerType.Fields.Add(globalServiceProvider);

        PropertyDefinition property = GenerateGlobalServiceProviderProperty(globalServiceProvider);
        containerType.Properties.Add(property);
        getGlobalServiceProvider = property.GetMethod;
        containerType.Methods.Add(property.GetMethod);

        MethodDefinition configureMethod = GenerateConfigureMethod(mapping, containerType);
        containerType.Methods.Add(configureMethod);

        MethodDefinition initMethod = GenerateInitMethod(configureMethod, globalServiceProvider);
        containerType.Methods.Add(initMethod);

        MethodDefinition disposeMethod = GenerateDisposeMethod(globalServiceProvider);
        containerType.Methods.Add(disposeMethod);

        return containerType;
    }

    private PropertyDefinition GenerateGlobalServiceProviderProperty(FieldDefinition backingField)
    {
        var property = new PropertyDefinition("Global", PropertyAttributes.None,
            ModuleDefinition.Get<IServiceProvider>());

        var getMethod = new MethodDefinition($"get_{property.Name}",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static |
            MethodAttributes.SpecialName, ModuleDefinition.Get<IServiceProvider>());
        property.GetMethod = getMethod;

        ILProcessor processor = getMethod.Body.GetILProcessor();
        Instruction loadField = Instruction.Create(OpCodes.Ldsfld, backingField);
        processor.Emit(OpCodes.Ldsfld, backingField);
        processor.Emit(OpCodes.Brtrue_S, loadField);

        processor.Emit(OpCodes.Ldstr, "AutoDI has not been initialized");
        processor.Emit(OpCodes.Newobj, ModuleDefinition.GetConstructor<InvalidOperationException>(typeof(string)));
        processor.Emit(OpCodes.Throw);

        processor.Append(loadField);
        processor.Emit(OpCodes.Ret);
        return property;
    }

    private MethodDefinition GenerateConfigureMethod(Mapping mapping, TypeDefinition containerType)
    {
        var method = new MethodDefinition("Gen_Configured",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            ModuleDefinition.ImportReference(typeof(void)));

        var serviceCollection = new ParameterDefinition("collection", ParameterAttributes.None, ModuleDefinition.Get<IServiceCollection>());
        method.Parameters.Add(serviceCollection);

        ILProcessor processor = method.Body.GetILProcessor();

        MethodReference addAuotDIServiceMethod = ModuleDefinition.GetMethod(typeof(ServiceCollectionMixins),
            nameof(ServiceCollectionMixins.AddAutoDIService));

        MethodDefinition funcCtor = ModuleDefinition.ResolveCoreConstructor(typeof(Func<,>));

        MethodReference getTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

        int factoryIndex = 0;
        foreach (TypeMap map in mapping)
        {
            try
            {
                InternalLogDebug($"Processing map for {map.TargetType.FullName}", DebugLogLevel.Verbose);

                MethodDefinition factoryMethod = GenerateFactoryMethod(map.TargetType, factoryIndex);
                if (factoryMethod == null)
                {
                    InternalLogDebug($"No acceptable constructor for '{map.TargetType.FullName}', skipping map", DebugLogLevel.Verbose);
                    continue;
                }
                containerType.Methods.Add(factoryMethod);
                factoryIndex++;

                processor.Emit(OpCodes.Ldarg_0); //collection parameter

                processor.Emit(OpCodes.Ldnull);
                processor.Emit(OpCodes.Ldftn, factoryMethod);
                processor.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(funcCtor.MakeGenericTypeConstructor(ModuleDefinition.Get<IServiceProvider>(), map.TargetType)));

                processor.Emit(OpCodes.Ldc_I4, map.Keys.Count);
                processor.Emit(OpCodes.Newarr, ModuleDefinition.Get<Type>());

                int arrayIndex = 0;
                foreach (TypeDefinition key in map.Keys)
                {
                    TypeReference importedKey = ModuleDefinition.ImportReference(key);
                    InternalLogDebug($"Mapping {importedKey.FullName} => {map.TargetType.FullName} ({map.Lifetime})", DebugLogLevel.Default);
                    processor.Emit(OpCodes.Dup);
                    processor.Emit(OpCodes.Ldc_I4, arrayIndex++);
                    processor.Emit(OpCodes.Ldtoken, importedKey);
                    processor.Emit(OpCodes.Call, getTypeMethod);
                    processor.Emit(OpCodes.Stelem_Ref);
                }

                processor.Emit(OpCodes.Ldc_I4, (int)map.Lifetime);

                var genericAddMethod = new GenericInstanceMethod(addAuotDIServiceMethod);
                genericAddMethod.GenericArguments.Add(map.TargetType);
                processor.Emit(OpCodes.Call, genericAddMethod);
            }
            catch (Exception e)
            {
                LogWarning($"Failed to create map for {map}\r\n{e}");
            }
        }

        processor.Emit(OpCodes.Ret);

        return method;
    }

    private MethodDefinition GenerateFactoryMethod(TypeDefinition targetType, int index)
    {
        //TODO: allow for specifying which constructor to use
        MethodDefinition targetTypeCtor = targetType.GetConstructors().OrderByDescending(c => c.Parameters.Count)
            .FirstOrDefault();

        if (targetTypeCtor == null) return null;

        var factory = new MethodDefinition($"<{targetType.Name}>_generated_{index}",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            targetType);
        factory.Parameters.Add(new ParameterDefinition("servicePProvider", ParameterAttributes.None, ModuleDefinition.Get<IServiceProvider>()));

        ILProcessor factoryProcessor = factory.Body.GetILProcessor();

        MethodReference getServiceMethod = ModuleDefinition.GetMethod(typeof(ServiceProviderServiceExtensions),
            nameof(ServiceProviderServiceExtensions.GetService));

        foreach (ParameterDefinition parameter in targetTypeCtor.Parameters)
        {
            factoryProcessor.Emit(OpCodes.Ldarg_0);
            var genericGetService = new GenericInstanceMethod(getServiceMethod);
            genericGetService.GenericArguments.Add(parameter.ParameterType);
            factoryProcessor.Emit(OpCodes.Call, genericGetService);
        }

        factoryProcessor.Emit(OpCodes.Newobj, targetTypeCtor);
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
        initProcessor.Emit(OpCodes.Newobj, ModuleDefinition.GetDefaultConstructor<ApplicationBuilder>());
        initProcessor.Emit(OpCodes.Stloc_0);

        initProcessor.Emit(OpCodes.Ldloc_0);
        initProcessor.Emit(OpCodes.Ldnull);
        initProcessor.Emit(OpCodes.Ldftn, configureMethod);
        initProcessor.Emit(OpCodes.Newobj, ModuleDefinition.GetConstructor<Action<IServiceCollection>>());
        initProcessor.Emit(OpCodes.Callvirt, ModuleDefinition.GetMethod<IApplicationBuilder>(nameof(IApplicationBuilder.ConfigureServices)));
        initProcessor.Emit(OpCodes.Pop);

        Instruction loadForBuild = Instruction.Create(OpCodes.Ldloc_0);

        initProcessor.Emit(OpCodes.Ldarg_0);
        initProcessor.Emit(OpCodes.Brfalse_S, loadForBuild);
        initProcessor.Emit(OpCodes.Ldarg_0);
        initProcessor.Emit(OpCodes.Ldloc_0);
        initProcessor.Emit(OpCodes.Callvirt, ModuleDefinition.GetMethod<Action<IApplicationBuilder>>(nameof(Action<IApplicationBuilder>.Invoke)));


        initProcessor.Append(loadForBuild);
        initProcessor.Emit(OpCodes.Callvirt, ModuleDefinition.GetMethod<IApplicationBuilder>(nameof(IApplicationBuilder.Build)));
        initProcessor.Emit(OpCodes.Stsfld, globalServiceProvider);

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
        Instruction loadNull = Instruction.Create(OpCodes.Ldnull);

        processor.Emit(OpCodes.Ldsfld, globalServiceProvider);
        processor.Emit(OpCodes.Isinst, ModuleDefinition.Get<IDisposable>());
        processor.Emit(OpCodes.Dup);
        processor.Emit(OpCodes.Stloc_0); //disposable
        processor.Emit(OpCodes.Brfalse_S, loadNull);
        processor.Emit(OpCodes.Ldloc_0); //disposable
        processor.Emit(OpCodes.Callvirt, ModuleDefinition.GetMethod<IDisposable>(nameof(IDisposable.Dispose)));


        processor.Append(loadNull);
        processor.Emit(OpCodes.Stsfld, globalServiceProvider);

        processor.Emit(OpCodes.Ret);
        return disposeMethod;
    }
}