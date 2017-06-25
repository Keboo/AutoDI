using AutoDI.Container;
using AutoDI.Container.Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using AutoDI;

[assembly: InternalsVisibleTo("AutoDI.Container.Tests")]
// ReSharper disable once CheckNamespace
public class ModuleWeaver
{
    // Will contain the full element XML from FodyWeavers.xml. OPTIONAL
    public XElement Config { get; set; }

    // Will log an MessageImportance.Normal message to MSBuild. OPTIONAL
    public Action<string> LogDebug { get; set; }

    // Will log an MessageImportance.High message to MSBuild. OPTIONAL
    public Action<string> LogInfo { get; set; }

    // Will log an warning message to MSBuild. OPTIONAL
    public Action<string> LogWarning { get; set; }

    // Will log an warning message to MSBuild at a specific point in the code. OPTIONAL
    public Action<string, SequencePoint> LogWarningPoint { get; set; }

    // Will log an error message to MSBuild. OPTIONAL
    public Action<string> LogError { get; set; }

    // Will log an error message to MSBuild at a specific point in the code. OPTIONAL
    public Action<string, SequencePoint> LogErrorPoint { get; set; }

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

    public ModuleWeaver()
    {
        LogWarning = s => { };
        LogInfo = s => { };
        LogDebug = s => { };
        LogError = s => { };
    }

    public void Execute()
    {
        try
        {
            Settings settings = Settings.Parse(Config);
            Mapping mapping = GetMapping(settings);

            TypeDefinition resolverType = CreateAutoDIContainer(mapping);
            ModuleDefinition.Types.Add(resolverType);

            if (settings.InjectContainer)
            {
                InjectContainer(resolverType);
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
                new[] {typeof(IDependencyResolver)}));
            var set = Instruction.Create(OpCodes.Call, setMethod);
            entryMethodProcessor.InsertBefore(ModuleDefinition.EntryPoint.Body.Instructions.First(), set);
            entryMethodProcessor.InsertBefore(set, create);
        }
        else
        {
            LogDebug($"No entry point in {ModuleDefinition.FileName}. Skipping container injection.");
        }
    }

    private Mapping GetMapping(Settings settings)
    {
        var rv = new Mapping();

        if (settings.Behavior.HasFlag(Behaviors.SingleInterfaceImplementation))
        {
            AddSingleInterfaceImplementation(rv);
        }
        if (settings.Behavior.HasFlag(Behaviors.IncludeClasses))
        {
            AddClasses(rv);
        }
        if (settings.Behavior.HasFlag(Behaviors.IncludeDerivedClasses))
        {
            AddDerivedClasses(rv);
        }

        AddSettingsMap(settings, rv);

        return rv;
    }

    private void AddSettingsMap(Settings settings, Mapping map)
    {
        var allTypes = ModuleDefinition.GetAllTypes().ToDictionary(x => x.FullName);

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

    private void AddClasses(Mapping map)
    {
        foreach (TypeDefinition type in ModuleDefinition.GetAllTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            map.Add(type, type, DuplicateKeyBehavior.RemoveAll);
        }
    }

    private void AddDerivedClasses(Mapping map)
    {
        TypeDefinition GetBaseType(TypeDefinition type)
        {
            return type.BaseType?.Resolve();
        }

        foreach (TypeDefinition type in ModuleDefinition.GetAllTypes().Where(t => t.IsClass && !t.IsAbstract && t.BaseType != null))
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

    private void AddSingleInterfaceImplementation(Mapping map)
    {
        var types = new Dictionary<string, List<TypeDefinition>>();

        foreach (TypeDefinition type in ModuleDefinition.GetAllTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var @interface in type.Interfaces)
            {
                if (!types.TryGetValue(@interface.InterfaceType.FullName, out List<TypeDefinition> list))
                {
                    types.Add(@interface.InterfaceType.FullName, list = new List<TypeDefinition>());
                }
                list.Add(type);
                //TODO: Base types
            }
        }

        foreach (KeyValuePair<string, List<TypeDefinition>> kvp in types)
        {
            if (kvp.Value.Count != 1) continue;
            var type = ModuleDefinition.GetType(kvp.Key);
            if (type == null) continue;
            map.Add(type, kvp.Value[0], DuplicateKeyBehavior.RemoveAll);
        }
    }

    private TypeDefinition CreateAutoDIContainer(Mapping mapping)
    {
        var type = new TypeDefinition("AutoDI", "AutoDIContainer",
            TypeAttributes.Class | TypeAttributes.Public)
        {
            BaseType = ModuleDefinition.Get<object>()
        };
        MethodDefinition ctor = CreateConstructor();
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

    private MethodDefinition CreateConstructor()
    {
        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, ModuleDefinition.ImportReference(typeof(void)));
        ILProcessor processor = ctor.Body.GetILProcessor();
        processor.Emit(OpCodes.Ldarg_0); //this
        processor.Emit(OpCodes.Call, ModuleDefinition.ImportReference(typeof(object).GetConstructor(new Type[0])));
        processor.Emit(OpCodes.Ret);
        return ctor;
    }

    private void BuildMap(FieldDefinition mapField, ILProcessor staticBody, TypeDefinition delegateContainer, Mapping mapping)
    {
        bool InvokeConstructor(TypeMap map, ILProcessor processor)
        {
            foreach (MethodDefinition targetTypeCtor in map.TargetType.GetConstructors().OrderByDescending(c => c.Parameters.Count))
            {
                if (targetTypeCtor.Parameters.All(pd => pd.HasDefault && pd.Constant == null))
                {
                    for (int i = 0; i < targetTypeCtor.Parameters.Count; i++)
                    {
                        processor.Emit(OpCodes.Ldnull);
                    }
                    processor.Emit(OpCodes.Newobj, targetTypeCtor);
                    return true;
                }
            }
            return false;
        }

        //TODO: Make static
        MethodReference addSingleton =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddSingleton)));
        MethodReference addLazySingleton =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddLazySingleton)));
        MethodReference addWeakTransient =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddWeakTransient)));
        MethodReference addTransient =
            ModuleDefinition.ImportReference(typeof(ContainerMap).GetMethod(nameof(ContainerMap.AddTransient)));

        TypeReference funcType = ModuleDefinition.ImportReference(typeof(Func<>));
        TypeReference type = ModuleDefinition.ImportReference(typeof(Type));
        MethodReference getTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

        int delegateMethodCount = 0;

        foreach (TypeMap map in mapping)
        {
            staticBody.Emit(OpCodes.Ldsfld, mapField);

            switch (map.CreateType)
            {
                case Create.Singleton:
                    if (!InvokeConstructor(map, staticBody))
                    {
                        staticBody.Remove(staticBody.Body.Instructions.Last());
                        LogDebug($"No acceptable constructor for '{map.TargetType.FullName}', skipping map");
                        continue;
                    }
                    break;
                default:
                    var delegateMethod = new MethodDefinition($"<{map.TargetType.Name}>_generated_{delegateMethodCount++}", MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Private, map.TargetType);
                    ILProcessor delegateProcessor = delegateMethod.Body.GetILProcessor();
                    if (!InvokeConstructor(map, delegateProcessor))
                    {
                        delegateMethodCount--;
                        staticBody.Remove(staticBody.Body.Instructions.Last());
                        LogDebug($"No acceptable constructor for '{map.TargetType.FullName}', skipping map");
                        continue;
                    }
                    delegateProcessor.Emit(OpCodes.Ret);
                    delegateContainer.Methods.Add(delegateMethod);

                    staticBody.Emit(OpCodes.Ldnull);
                    staticBody.Emit(OpCodes.Ldftn, delegateMethod);

                    MethodReference funcCtor = ModuleDefinition.ImportReference(typeof(Func<>).GetConstructors().Single());
                    funcCtor.DeclaringType = funcType.MakeGenericInstanceType(map.TargetType);

                    staticBody.Emit(OpCodes.Newobj, funcCtor);
                    break;
            }

            staticBody.Emit(OpCodes.Ldc_I4, map.Keys.Count);
            staticBody.Emit(OpCodes.Newarr, type);

            int arrayIndex = 0;
            foreach (TypeDefinition key in map.Keys)
            {
                TypeReference importedKey = ModuleDefinition.ImportReference(key);
                LogDebug($"Mapping {importedKey.FullName} -> {map.TargetType.FullName}");
                staticBody.Emit(OpCodes.Dup);
                staticBody.Emit(OpCodes.Ldc_I4, arrayIndex++);
                staticBody.Emit(OpCodes.Ldtoken, importedKey);
                staticBody.Emit(OpCodes.Call, getTypeMethod);
                staticBody.Emit(OpCodes.Stelem_Ref);
            }

            MethodReference addMethod;
            switch (map.CreateType)
            {
                case Create.Singleton:
                    addMethod = addSingleton;
                    break;
                case Create.LazySingleton:
                    addMethod = addLazySingleton;
                    break;
                case Create.WeakTransient:
                    addMethod = addWeakTransient;
                    break;
                case Create.Transient:
                    addMethod = addTransient;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid Crate value '{map.CreateType}'");
            }
            var addGenericMethod = new GenericInstanceMethod(addMethod);
            addGenericMethod.GenericArguments.Add(map.TargetType);

            staticBody.Emit(OpCodes.Call, addGenericMethod);
            staticBody.Emit(OpCodes.Nop);

        }
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
        LogDebug($"'{targetType.FullName}' cannot be cast to '{key.FullName}', ignoring");
        return false;
    }

    private class TypeMap
    {
        public TypeMap(TypeDefinition targetType)
        {
            TargetType = targetType;
        }

        public Create CreateType { get; set; } = Create.LazySingleton;

        public TypeDefinition TargetType { get; }

        public ICollection<TypeDefinition> Keys { get; } = new HashSet<TypeDefinition>(TypeComparer.FullName);
    }

    private enum DuplicateKeyBehavior
    {
        Replace,
        RemoveAll
    }

    private class Mapping : IEnumerable<TypeMap>
    {
        private readonly Dictionary<string, TypeMap> _maps = new Dictionary<string, TypeMap>();

        public void Add(TypeDefinition key, TypeDefinition targetType, DuplicateKeyBehavior behavior)
        {
            if (!HasValidConstructor(targetType)) return;

            //Last key in wins, this allows for manual mapping to override things added with behaviors
            bool duplicateKey = false;
            foreach (var kvp in _maps.Where(kvp => kvp.Value.Keys.Contains(key)).ToList())
            {
                duplicateKey = true;
                kvp.Value.Keys.Remove(key);
                if (!kvp.Value.Keys.Any())
                {
                    _maps.Remove(kvp.Key);
                }
            }

            if (duplicateKey && behavior == DuplicateKeyBehavior.RemoveAll)
            {
                return;
            }

            if (!_maps.TryGetValue(targetType.FullName, out TypeMap typeMap))
            {
                _maps[targetType.FullName] = typeMap = new TypeMap(targetType);
            }

            typeMap.Keys.Add(key);
        }

        public void UpdateCreation(ICollection<MatchType> matchTypes)
        {
            foreach (string targetType in _maps.Keys.ToList())
            {
                foreach (MatchType type in matchTypes)
                {
                    if (type.Matches(targetType))
                    {
                        switch (type.Create)
                        {
                            case Create.None:
                                _maps.Remove(targetType);
                                break;
                            default:
                                _maps[targetType].CreateType = type.Create;
                                break;
                        }
                    }
                }
            }
        }

        //TODO: This behavior is duplicated when it builds the map :/
        private bool HasValidConstructor(TypeDefinition type)
        {
            return type.GetConstructors().Any(c => c.Parameters.All(pd => pd.HasDefault && pd.Constant == null));
        }

        public IEnumerator<TypeMap> GetEnumerator()
        {
            return _maps.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // Will be called when a request to cancel the build occurs. OPTIONAL
    //public void Cancel()
    //{
    //
    //}

    // Will be called after all weaving has occurred and the module has been saved. OPTIONAL
    //public void AfterWeaving()
    //{
    //}
}

