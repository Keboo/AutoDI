using AutoDI;
using AutoDI.Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using DependencyAttribute = AutoDI.DependencyAttribute;
using OpCodes = Mono.Cecil.Cil.OpCodes;

[assembly: InternalsVisibleTo("AutoDI.Fody.Tests")]
// ReSharper disable once CheckNamespace
public class ModuleWeaver
{
    // Will contain the full element XML from FodyWeavers.xml. OPTIONAL
    public XElement Config { get; set; }

    // Will log an MessageImportance.Normal message to MSBuild. OPTIONAL
    public Action<string> LogDebug { get; set; } = s => { };

    // Will log an MessageImportance.High message to MSBuild. OPTIONAL
    public Action<string> LogInfo { get; set; } = s => { };

    // Will log an warning message to MSBuild. OPTIONAL
    public Action<string> LogWarning { get; set; } = s => { };

    // Will log an warning message to MSBuild at a specific point in the code. OPTIONAL
    public Action<string, SequencePoint> LogWarningPoint { get; set; } = (s, p) => { };

    // Will log an error message to MSBuild. OPTIONAL
    public Action<string> LogError { get; set; } = s => { };

    // Will log an error message to MSBuild at a specific point in the code. OPTIONAL
    public Action<string, SequencePoint> LogErrorPoint { get; set; } = (s, p) => { };

    // An instance of Mono.Cecil.IAssemblyResolver for resolving assembly references. OPTIONAL
    public IAssemblyResolver AssemblyResolver { get; set; }

    // An instance of Mono.Cecil.ModuleDefinition for processing. REQUIRED
    public ModuleDefinition ModuleDefinition { get; set; }

    // Will contain the full path of the target assembly. OPTIONAL
    public string AssemblyFilePath { get; set; }

    // Will contain the full directory path of the target project. 
    // A copy of $(ProjectDir). OPTIONAL
    public string ProjectDirectoryPath { get; set; }

    // Will contain the full directory path of the current weaver. OPTIONAL
    public string AddinDirectoryPath { get; set; }

    // Will contain the full directory path of the current solution.
    // A copy of `$(SolutionDir)` or, if it does not exist, a copy of `$(MSBuildProjectDirectory)..\..\..\`. OPTIONAL
    public string SolutionDirectoryPath { get; set; }

    // Will contain a semicomma delimetered string that contains 
    // all the references for the target project. 
    // A copy of the contents of the @(ReferencePath). OPTIONAL
    public string References { get; set; }

    // Will a list of all the references marked as copy-local. 
    // A copy of the contents of the @(ReferenceCopyLocalPaths). OPTIONAL
    public List<string> ReferenceCopyLocalPaths { get; set; }

    // Will a list of all the msbuild constants. 
    // A copy of the contents of the $(DefineConstants). OPTIONAL
    public List<string> DefineConstants { get; set; }

    public string AssemblyToProcess { get; set; }

    private Action<string, DebugLogLevel> InternalLogDebug { get; set; } = (s, l) => { };

    public void Execute()
    {
        try
        {
            Settings settings = Settings.Parse(Config);
            InternalLogDebug = (s, l) =>
            {
                if (l <= settings.DebugLogLevel)
                {
                    LogDebug(s);
                }
            };

            Mapping mapping = GetMapping(settings);
            InternalLogDebug($"Found potential map:\r\n{mapping}", DebugLogLevel.Verbose);
            TypeDefinition resolverType = CreateAutoDIContainer(mapping);

            ModuleDefinition.Types.Add(resolverType);

            if (settings.InjectContainer)
            {
                InjectContainer(resolverType);
            }

            foreach (TypeDefinition type in ModuleDefinition.GetAllTypes())
            {
                ProcessType(type);
            }
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
                sb.AppendLine(e.ToString());
            LogError(sb.ToString());
        }
    }

    private void InjectContainer(TypeDefinition resolverType)
    {
        if (ModuleDefinition.EntryPoint != null)
        {
            ILProcessor entryMethodProcessor = ModuleDefinition.EntryPoint.Body.GetILProcessor();
            var create = Instruction.Create(OpCodes.Newobj,
                resolverType.Methods.Single(m => m.IsConstructor && !m.IsStatic));
            var setMethod = ModuleDefinition.ImportReference(typeof(DependencyResolver).GetMethod(
                nameof(DependencyResolver.Set),
                new[] { typeof(IDependencyResolver) }));
            var set = Instruction.Create(OpCodes.Call, setMethod);
            entryMethodProcessor.InsertBefore(ModuleDefinition.EntryPoint.Body.Instructions.First(), set);
            entryMethodProcessor.InsertBefore(set, create);
        }
        else
        {
            InternalLogDebug($"No entry point in {ModuleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
        }
    }

    private Mapping GetMapping(Settings settings)
    {
        var rv = new Mapping();
        ICollection<TypeDefinition> allTypes = GetAllTypes(settings);

        if (settings.Behavior.HasFlag(Behaviors.SingleInterfaceImplementation))
        {
            AddSingleInterfaceImplementation(rv, allTypes);
        }
        if (settings.Behavior.HasFlag(Behaviors.IncludeClasses))
        {
            AddClasses(rv, allTypes);
        }
        if (settings.Behavior.HasFlag(Behaviors.IncludeBaseClasses))
        {
            AddBaseClasses(rv, allTypes);
        }

        AddSettingsMap(settings, rv, allTypes);

        return rv;
    }

    private ICollection<TypeDefinition> GetAllTypes(Settings settings)
    {
        var allTypes = new HashSet<TypeDefinition>(TypeComparer.FullName);
        IEnumerable<TypeDefinition> FilterTypes(IEnumerable<TypeDefinition> types) => 
            types.Where(t => !t.IsCompilerGenerated() && !allTypes.Remove(t));

        foreach (TypeDefinition type in FilterTypes(ModuleDefinition.GetAllTypes()))
        {
            allTypes.Add(type);
        }
        foreach (AssemblyNameReference assemblyReference in ModuleDefinition.AssemblyReferences)
        {
            bool useAutoDiAssebmlies = settings.Behavior.HasFlag(Behaviors.IncludeDependentAutoDIAssemblies);
            bool matchesAssembly = settings.Assemblies.Any(a => a.Matches(assemblyReference.FullName));
            if (useAutoDiAssebmlies || matchesAssembly)
            {
                AssemblyDefinition assembly = AssemblyResolver.Resolve(assemblyReference);
                if (assembly == null) continue;
                
                //Check if it references AutoDI. If it doesn't we will skip
                if (!matchesAssembly && assembly.MainModule.AssemblyReferences.All(a =>
                        a.FullName != typeof(DependencyAttribute).Assembly.FullName))
                {
                    continue;
                }
                //Either references AutoDI, or was a config assembly match, include the types.
                foreach (TypeDefinition type in FilterTypes(assembly.MainModule.GetAllTypes()))
                {
                    allTypes.Add(type);
                }
            }
        }
        return allTypes;
    }

    private static void AddSingleInterfaceImplementation(Mapping map, IEnumerable<TypeDefinition> allTypes)
    {
        var types = new Dictionary<TypeReference, List<TypeDefinition>>(TypeComparer.FullName);

        foreach (TypeDefinition type in allTypes.Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var @interface in type.Interfaces)
            {
                if (!types.TryGetValue(@interface.InterfaceType, out List<TypeDefinition> list))
                {
                    types.Add(@interface.InterfaceType, list = new List<TypeDefinition>());
                }
                list.Add(type);
                //TODO: Base types
            }
        }

        foreach (KeyValuePair<TypeReference, List<TypeDefinition>> kvp in types)
        {
            if (kvp.Value.Count != 1) continue;
            map.Add(kvp.Key.Resolve(), kvp.Value[0], DuplicateKeyBehavior.RemoveAll);
        }
    }

    private static void AddClasses(Mapping map, IEnumerable<TypeDefinition> types)
    {
        foreach (TypeDefinition type in types.Where(t => t.IsClass && !t.IsAbstract))
        {
            map.Add(type, type, DuplicateKeyBehavior.RemoveAll);
        }
    }

    private static void AddBaseClasses(Mapping map, IEnumerable<TypeDefinition> types)
    {
        TypeDefinition GetBaseType(TypeDefinition type)
        {
            return type.BaseType?.Resolve();
        }

        foreach (TypeDefinition type in types.Where(t => t.IsClass && !t.IsAbstract && t.BaseType != null))
        {
            for (TypeDefinition t = GetBaseType(type); t != null; t = GetBaseType(t))
            {
                if (t.FullName != typeof(object).FullName)
                {
                    map.Add(t, type, DuplicateKeyBehavior.RemoveAll);
                }
            }
        }
    }

    private void AddSettingsMap(Settings settings, Mapping map, IEnumerable<TypeDefinition> types)
    {
        var allTypes = types.ToDictionary(x => x.FullName);

        foreach (string typeName in allTypes.Keys)
        {
            foreach (Map settingsMap in settings.Maps)
            {
                //TODO: Logging for the various cases where we don't map...
                if (settingsMap.TryGetMap(typeName, out string mappedType) &&
                    allTypes.TryGetValue(mappedType, out TypeDefinition mapped) &&
                    (settingsMap.Force || CanBeCastToType(allTypes[typeName], mapped)))
                {
                    map.Add(allTypes[typeName], mapped, DuplicateKeyBehavior.Replace);
                }
            }
        }
        map.UpdateCreation(settings.Types);
    }

    private bool CanBeCastToType(TypeDefinition key, TypeDefinition targetType)
    {
        var comparer = TypeComparer.FullName;

        for (TypeDefinition t = targetType; t != null; t = t.BaseType?.Resolve())
        {
            if (comparer.Equals(key, t)) return true;
            if (t.Interfaces.Any(i => comparer.Equals(i.InterfaceType, key)))
            {
                return true;
            }
        }
        InternalLogDebug($"'{targetType.FullName}' cannot be cast to '{key.FullName}', ignoring", DebugLogLevel.Verbose);
        return false;
    }

    private TypeDefinition CreateAutoDIContainer(Mapping mapping)
    {
        var type = new TypeDefinition("AutoDI", "AutoDIContainer",
            TypeAttributes.Class | TypeAttributes.Public)
        {
            BaseType = ModuleDefinition.Get<object>()
        };
        MethodDefinition ctor = CreateContainerConstructor();
        type.Methods.Add(ctor);


        //Create static constructor
        MethodDefinition staticConstructor = CreateStaticConstructor();
        ILProcessor staticBody = staticConstructor.Body.GetILProcessor();

        //Declare and initialize dictionary map
        FieldDefinition mapField = CreateStaticReadonlyField<ContainerMap>("_items", false);
        type.Fields.Add(mapField);
        MethodReference mapConstructor = ModuleDefinition.ImportReference(typeof(ContainerMap).GetConstructor(new Type[0]));
        staticBody.Emit(OpCodes.Newobj, mapConstructor);
        staticBody.Emit(OpCodes.Stsfld, mapField);

        BuildMap(mapField, staticBody, type, mapping);

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

        type.Methods.Add(staticConstructor);

        //Implement IDependencyResolver interface
        TypeReference dependencyResolver = ModuleDefinition.Get<IDependencyResolver>();
        type.Interfaces.Add(new InterfaceImplementation(dependencyResolver));

        //Create resolve method
        var resolveMethod = new MethodDefinition("Resolve",
            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual |
            MethodAttributes.NewSlot
            , type);
        resolveMethod.Parameters.Add(new ParameterDefinition("parameters", ParameterAttributes.None, ModuleDefinition.Get<object[]>()));

        resolveMethod.Overrides.Add(ModuleDefinition.ImportReference(dependencyResolver.Resolve().Methods.Single()));

        var genericParameter = new GenericParameter("T", resolveMethod);
        resolveMethod.GenericParameters.Add(genericParameter);
        resolveMethod.ReturnType = genericParameter;

        var lazy = new VariableDefinition(ModuleDefinition.Get<Lazy<object>>());
        resolveMethod.Body.Variables.Add(lazy);
        var genericVariable = new VariableDefinition(genericParameter);
        resolveMethod.Body.Variables.Add(genericVariable);

        var body = resolveMethod.Body.GetILProcessor();
        body.Emit(OpCodes.Ldsfld, mapField);

        MethodReference getMethod =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.Get)));
        var genericGetMethod = new GenericInstanceMethod(getMethod);
        genericGetMethod.GenericArguments.Add(genericParameter);

        body.Emit(OpCodes.Call, genericGetMethod);
        body.Emit(OpCodes.Ret);

        type.Methods.Add(resolveMethod);

        return type;
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

                        staticBody.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(GetGenericTypeConstructor(funcCtor, targetType)));
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

    //Based on example from here: https://stackoverflow.com/questions/16430947/emit-call-to-system-lazyt-constructor-with-mono-cecil
    public static MethodReference GetGenericTypeConstructor(MethodReference self, params TypeReference[] args)
    {
        var reference = new MethodReference(
            self.Name,
            self.ReturnType,
            self.DeclaringType.MakeGenericInstanceType(args))
        {
            HasThis = self.HasThis,
            ExplicitThis = self.ExplicitThis,
            CallingConvention = self.CallingConvention
        };

        foreach (var parameter in self.Parameters)
        {
            reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
        }

        foreach (var genericParam in self.GenericParameters)
        {
            reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
        }

        return reference;
    }

    private MethodDefinition CreateContainerConstructor()
    {
        var ctor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName, ModuleDefinition.ImportReference(typeof(void)));
        ILProcessor processor = ctor.Body.GetILProcessor();
        processor.Emit(OpCodes.Ldarg_0); //this
        processor.Emit(OpCodes.Call, ModuleDefinition.ImportReference(typeof(object).GetConstructor(new Type[0])));
        processor.Emit(OpCodes.Ret);
        return ctor;
    }

    private MethodDefinition CreateStaticConstructor()
    {
        var ctor = new MethodDefinition(".cctor",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, ModuleDefinition.ImportReference(typeof(void)));

        return ctor;
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

    private void ProcessType(TypeDefinition type)
    {
        foreach (MethodDefinition ctor in type.Methods.Where(x => x.IsConstructor))
        {
            ProcessConstructor(type, ctor);
        }
    }

    private void ProcessConstructor(TypeDefinition type, MethodDefinition constructor)
    {
        var dependencyParameters = constructor.Parameters.Where(
                        p => p.CustomAttributes.Any(a => a.AttributeType.IsType<DependencyAttribute>())).ToList();

        if (dependencyParameters.Any())
        {
            var resolverType = ModuleDefinition.Get<IDependencyResolver>();
            var dependencyResolverType = ModuleDefinition.ImportReference(typeof(DependencyResolver));
            var typeReference = ModuleDefinition.Get<Type>();
            var getTypeMethod = ModuleDefinition.ImportReference(new MethodReference(nameof(Type.GetTypeFromHandle), typeReference, typeReference)
            {
                Parameters = { new ParameterDefinition(ModuleDefinition.ImportReference(typeof(RuntimeTypeHandle))) }
            });
            var resolverRequestType = ModuleDefinition.Get<ResolverRequest>();
            var resolverRequestCtor = ModuleDefinition.ImportReference(typeof(ResolverRequest).GetConstructor(new[] { typeof(Type), typeof(Type[]) }));

            var injector = new Injector(constructor);

            var end = Instruction.Create(OpCodes.Nop);

            var resolverVariable = new VariableDefinition(resolverType);
            constructor.Body.Variables.Add(resolverVariable);
            var resolverRequestVariable = new VariableDefinition(resolverRequestType);
            constructor.Body.Variables.Add(resolverRequestVariable);

            //Create the ResolverRequest
            //Get the calling type
            injector.Insert(OpCodes.Ldtoken, type);
            injector.Insert(OpCodes.Call, getTypeMethod);
            //Create a new array to hold the dependency types
            injector.Insert(OpCodes.Ldc_I4, dependencyParameters.Count);
            injector.Insert(OpCodes.Newarr, typeReference);
            for (int i = 0; i < dependencyParameters.Count; ++i)
            {
                //Load the dependency type into the array
                injector.Insert(OpCodes.Dup);
                injector.Insert(Instruction.Create(OpCodes.Ldc_I4, i));
                TypeReference parameterType = ModuleDefinition.ImportReference(dependencyParameters[i].ParameterType);
                injector.Insert(OpCodes.Ldtoken, parameterType);
                injector.Insert(OpCodes.Call, getTypeMethod);
                injector.Insert(OpCodes.Stelem_Ref);
            }
            //Call the ResolverRequest constructor
            injector.Insert(OpCodes.Newobj, resolverRequestCtor);
            //Store the resolver request in the variable
            injector.Insert(OpCodes.Stloc, resolverRequestVariable);
            //Load the resolver request from the variable
            injector.Insert(OpCodes.Ldloc, resolverRequestVariable);

            //Get the IDependencyResolver by calling DependencyResolver.Get(ResolverRequest)
            injector.Insert(OpCodes.Call,
                new MethodReference(nameof(DependencyResolver.Get), resolverType, dependencyResolverType)
                {
                    Parameters = { new ParameterDefinition(resolverRequestType) }
                });
            //Store the resolver in our local variable
            injector.Insert(OpCodes.Stloc, resolverVariable);
            //Push the resolver on top of the stack
            injector.Insert(OpCodes.Ldloc, resolverVariable);
            //Branch to the end if the resolver is null
            injector.Insert(OpCodes.Brfalse, end);

            foreach (ParameterDefinition parameter in dependencyParameters)
            {
                if (!parameter.IsOptional)
                {
                    LogInfo(
                        $"Constructor parameter {parameter.ParameterType.Name} {parameter.Name} is marked with {nameof(DependencyAttribute)} but is not an optional parameter. In {type.FullName}.");
                }
                if (parameter.Constant != null)
                {
                    LogWarning(
                        $"Constructor parameter {parameter.ParameterType.Name} {parameter.Name} in {type.FullName} does not have a null default value. AutoDI will only resolve dependencies that are null");
                }

                TypeReference parameterType = ModuleDefinition.ImportReference(parameter.ParameterType);
                var afterParam = Instruction.Create(OpCodes.Nop);
                //Push dependency parameter onto the stack
                injector.Insert(OpCodes.Ldarg, parameter);
                //Push null onto the stack
                injector.Insert(OpCodes.Ldnull);
                //Push 1 if the values are equal, 0 if they are not equal
                injector.Insert(OpCodes.Ceq);
                //Branch if the value is false (0), the dependency was set by the caller we wont replace it
                injector.Insert(OpCodes.Brfalse_S, afterParam);
                //Push the dependency resolver onto the stack
                injector.Insert(OpCodes.Ldloc, resolverVariable);

                //Create parameters array
                var dependencyAttribute = parameter.CustomAttributes.First(x => x.AttributeType.IsType<DependencyAttribute>());
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
                var resolveMethod = ModuleDefinition.ImportReference(
                        typeof(IDependencyResolver).GetMethod(nameof(IDependencyResolver.Resolve)));
                resolveMethod = new GenericInstanceMethod(resolveMethod)
                {
                    GenericArguments = { parameterType }
                };
                injector.Insert(OpCodes.Callvirt, resolveMethod);
                //Store the return from the resolve method in the method parameter
                injector.Insert(OpCodes.Starg, parameter);
                injector.Insert(afterParam);
            }
            injector.Insert(end);

            constructor.Body.OptimizeMacros();
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
        LogWarning($"Unknown constant type {constant.GetType().FullName}");
    }


    // Will be called when a request to cancel the build occurs. OPTIONAL
    public void Cancel()
    {
    }

    // Will be called after all weaving has occurred and the module has been saved. OPTIONAL
    public void AfterWeaving()
    {
    }
}

