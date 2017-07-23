
using System;
using System.Linq;
using AutoDI;
using AutoDI.Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

// ReSharper disable once CheckNamespace
partial class ModuleWeaver
{
    private TypeDefinition CreateAutoDIContainer(Mapping mapping)
    {
        var containerType = new TypeDefinition("AutoDI", "AutoDIContainer",
            TypeAttributes.Class | TypeAttributes.Public)
        {
            BaseType = ModuleDefinition.Get<BaseResolver>()
        };
        MethodDefinition ctor = ModuleDefinition.CreateDefaultConstructor(typeof(BaseResolver));
        containerType.Methods.Add(ctor);


        //Create static constructor
        MethodDefinition staticConstructor = ModuleDefinition.CreateStaticConstructor();
        ILProcessor staticBody = staticConstructor.Body.GetILProcessor();

        //Declare and initialize dictionary map
        FieldDefinition mapField = CreateStaticReadonlyField<ContainerMap>("_items", false);
        containerType.Fields.Add(mapField);
        MethodReference mapConstructor = ModuleDefinition.ImportReference(typeof(ContainerMap).GetConstructor(new Type[0]));
        staticBody.Emit(OpCodes.Newobj, mapConstructor);
        staticBody.Emit(OpCodes.Stsfld, mapField);

        BuildMap(mapField, staticBody, containerType, mapping);

        //Pass map to setup method if one exists
        MethodDefinition setupMethod = FindSetupMethod();
        if (setupMethod != null)
        {
            InternalLogDebug($"Found setup method '{setupMethod.FullName}'", DebugLogLevel.Verbose);
            staticBody.Emit(OpCodes.Ldsfld, mapField);
            staticBody.Emit(OpCodes.Call, setupMethod);
            staticBody.Emit(OpCodes.Nop);
        }

        staticBody.Emit(OpCodes.Ret);

        containerType.Methods.Add(staticConstructor);

        //Override BaseResolver.Resolve(Type, params object[]) method.
        
        //Create resolve method
        var resolveMethod = new MethodDefinition("Resolve",
            MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual 
            , containerType);
        resolveMethod.Parameters.Add(new ParameterDefinition("desiredType", ParameterAttributes.None, ModuleDefinition.Get<Type>()));
        resolveMethod.Parameters.Add(new ParameterDefinition("parameters", ParameterAttributes.None, ModuleDefinition.Get<object[]>()));
        resolveMethod.ReturnType = ModuleDefinition.Get<object>();
        
        var body = resolveMethod.Body.GetILProcessor();
        body.Emit(OpCodes.Ldsfld, mapField);

        MethodReference getMethod =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.Get), new[] { typeof(Type) }));
        //TODO: parameters too
        body.Emit(OpCodes.Ldarg_1);
        body.Emit(OpCodes.Call, getMethod);
        body.Emit(OpCodes.Ret);

        containerType.Methods.Add(resolveMethod);

        return containerType;
    }

    private void BuildMap(FieldDefinition mapField, ILProcessor staticBody, TypeDefinition delegateContainer, Mapping mapping)
    {
        bool InvokeConstructor(TypeDefinition targetType, ILProcessor processor)
        {
            foreach (MethodDefinition targetTypeCtor in targetType.GetConstructors().OrderByDescending(c => c.Parameters.Count))
            {
                if (targetTypeCtor.Parameters.All(pd => pd.HasDefault && pd.Constant == null))
                {
                    for (int i = 0; i < targetTypeCtor.Parameters.Count; i++)
                    {
                        processor.Emit(OpCodes.Ldnull);
                    }
                    processor.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(targetTypeCtor));
                    return true;
                }
            }
            return false;
        }

        MethodReference addSingleton =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddSingleton)));
        MethodReference addLazySingleton =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddLazySingleton)));
        MethodReference addWeakTransient =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddWeakTransient)));
        MethodReference addTransient =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddTransient)));

        TypeReference funcType = ModuleDefinition.ImportReference(typeof(Func<>));
        //NB: This null fall back is due to an issue with Mono.Cecil 0.10.0
        //From GitHub issues it looks like it make be resolved in some of the beta builds, however this would require modifying Fody.
        //For now we will just manually resolve this way. Since we know Func<>> lives in the core library.
        TypeDefinition funcDefinition = funcType.Resolve() ?? ModuleDefinition.AssemblyResolver
                                            .Resolve((AssemblyNameReference)ModuleDefinition.TypeSystem.CoreLibrary)
                                            .MainModule.GetType(typeof(Func<>).FullName);
        if (funcDefinition == null)
        {
            LogError($"Failed to resolve type '{typeof(Func<>).FullName}'");
            return;
        }
        MethodDefinition funcCtor = funcDefinition.GetConstructors().Single();

        TypeReference type = ModuleDefinition.ImportReference(typeof(Type));
        MethodReference getTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

        int delegateMethodCount = 0;
        foreach (TypeMap map in mapping)
        {
            try
            {
                InternalLogDebug($"Processing map for {map.TargetType.FullName}", DebugLogLevel.Verbose);

                TypeDefinition targetType = map.TargetType;
                staticBody.Emit(OpCodes.Ldsfld, mapField);

                switch (map.Lifetime)
                {
                    case Lifetime.Singleton:
                        if (!InvokeConstructor(targetType, staticBody))
                        {
                            staticBody.Remove(staticBody.Body.Instructions.Last());
                            InternalLogDebug($"No acceptable constructor for '{targetType.FullName}', skipping map", DebugLogLevel.Verbose);
                            continue;
                        }
                        break;
                    default:
                        var delegateMethod = new MethodDefinition($"<{targetType.Name}>_generated_{delegateMethodCount++}",
                            MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static |
                            MethodAttributes.Private, ModuleDefinition.ImportReference(targetType));
                        ILProcessor delegateProcessor = delegateMethod.Body.GetILProcessor();
                        if (!InvokeConstructor(targetType, delegateProcessor))
                        {
                            delegateMethodCount--;
                            staticBody.Remove(staticBody.Body.Instructions.Last());
                            InternalLogDebug($"No acceptable constructor for '{targetType.FullName}', skipping map", DebugLogLevel.Verbose);
                            continue;
                        }
                        delegateProcessor.Emit(OpCodes.Ret);
                        delegateContainer.Methods.Add(delegateMethod);

                        staticBody.Emit(OpCodes.Ldnull);
                        staticBody.Emit(OpCodes.Ldftn, delegateMethod);

                        staticBody.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(funcCtor.MakeGenericType(targetType)));
                        break;
                }

                staticBody.Emit(OpCodes.Ldc_I4, map.Keys.Count);
                staticBody.Emit(OpCodes.Newarr, type);

                int arrayIndex = 0;
                foreach (TypeDefinition key in map.Keys)
                {
                    TypeReference importedKey = ModuleDefinition.ImportReference(key);
                    InternalLogDebug($"Mapping {importedKey.FullName} => {targetType.FullName} ({map.Lifetime})", DebugLogLevel.Default);
                    staticBody.Emit(OpCodes.Dup);
                    staticBody.Emit(OpCodes.Ldc_I4, arrayIndex++);
                    staticBody.Emit(OpCodes.Ldtoken, importedKey);
                    staticBody.Emit(OpCodes.Call, getTypeMethod);
                    staticBody.Emit(OpCodes.Stelem_Ref);
                }

                MethodReference addMethod;
                switch (map.Lifetime)
                {
                    case Lifetime.Singleton:
                        addMethod = addSingleton;
                        break;
                    case Lifetime.LazySingleton:
                        addMethod = addLazySingleton;
                        break;
                    case Lifetime.WeakTransient:
                        addMethod = addWeakTransient;
                        break;
                    case Lifetime.Transient:
                        addMethod = addTransient;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid Crate value '{map.Lifetime}'");
                }
                var addGenericMethod = new GenericInstanceMethod(addMethod);
                addGenericMethod.GenericArguments.Add(ModuleDefinition.ImportReference(targetType));

                staticBody.Emit(OpCodes.Call, addGenericMethod);
                staticBody.Emit(OpCodes.Nop);
            }
            catch (Exception e)
            {
                LogWarning($"Failed to create map for {map}\r\n{e}");
            }
        }
    }

    private MethodDefinition FindSetupMethod()
    {
        return ModuleDefinition
            .GetAllTypes()
            .SelectMany(t => t.GetMethods())
            .FirstOrDefault(md => md.IsStatic &&
                                  (md.IsPublic || md.IsAssembly) &&
                                  md.CustomAttributes.Any(a => a.AttributeType.IsType<SetupMethodAttribute>()) &&
                                  md.Parameters.Count == 1 &&
                                  md.Parameters[0].ParameterType.IsType<ContainerMap>());
    }

    private FieldDefinition CreateStaticReadonlyField<T>(string name, bool @public)
    {
        return CreateStaticReadonlyField(name, @public, ModuleDefinition.Get<T>());
    }

    private static FieldDefinition CreateStaticReadonlyField(string name, bool @public, TypeReference type)
    {
        return new FieldDefinition(name,
            (@public ? FieldAttributes.Public : FieldAttributes.Private) | FieldAttributes.Static |
            FieldAttributes.InitOnly, type);
    }
}