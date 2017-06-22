using AutoDI;
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

            InjectResolver(resolverType);
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
                sb.AppendLine(e.ToString());
            LogError(sb.ToString());
        }
    }

    private void InjectResolver(TypeDefinition resolverType)
    {
        if (ModuleDefinition.EntryPoint != null)
        {
            ILProcessor entryMethodProcessor = ModuleDefinition.EntryPoint.Body.GetILProcessor();
            var create = Instruction.Create(OpCodes.Newobj, resolverType.Methods.Single(m => m.IsConstructor && !m.IsStatic));
            var setMethod = ModuleDefinition.ImportReference(typeof(DependencyResolver).GetMethod(nameof(DependencyResolver.Set),
                new[] { typeof(IDependencyResolver) }));
            var set = Instruction.Create(OpCodes.Call, setMethod);
            entryMethodProcessor.InsertBefore(ModuleDefinition.EntryPoint.Body.Instructions.First(), set);
            entryMethodProcessor.InsertBefore(set, create);
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

        AddSettingsMap(settings, rv);

        return rv;
    }

    private void AddSettingsMap(Settings settings, Mapping map)
    {
        var allTypes = ModuleDefinition.GetAllTypes().ToDictionary(x => x.FullName);
        foreach (Map settingsMap in settings.Maps)
        {
            foreach (string typeName in allTypes.Keys)
            {
                if (settingsMap.TryGetMap(typeName, out string mappedType))
                {
                    if (allTypes.TryGetValue(mappedType, out TypeDefinition mapped))
                    {
                        map.Add(allTypes[typeName], mapped);
                    }
                }
            }
        }
    }

    private void AddClasses(Mapping map)
    {
        foreach (TypeDefinition type in ModuleDefinition.GetAllTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            map.Add(type, type);
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
            map.Add(type, kvp.Value[0]);
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
        FieldDefinition mapField = CreateStaticReadonlyField<InternalMap>("_items", false);
        type.Fields.Add(mapField);
        MethodReference mapConstructor = ModuleDefinition.ImportReference(typeof(InternalMap).GetConstructor(new Type[0]));
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

        var loadLazyInstruction = Instruction.Create(OpCodes.Ldloc_0);
        var returnInstruction = Instruction.Create(OpCodes.Ret);

        var body = resolveMethod.Body.GetILProcessor();
        body.Append(Instruction.Create(OpCodes.Ldsfld, mapField));
        MethodReference getMethod =
            ModuleDefinition.ImportReference(typeof(InternalMap).GetMethod(nameof(InternalMap.Get)));
        var genericGetMethod = new GenericInstanceMethod(getMethod);
        genericGetMethod.GenericArguments.Add(genericParameter);
        body.Append(Instruction.Create(OpCodes.Call, genericGetMethod));
        //body.Append(Instruction.Create(OpCodes.Ldtoken, genericParameter));
        //MethodReference getTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
        //body.Append(Instruction.Create(OpCodes.Call, getTypeMethod));
        //body.Append(Instruction.Create(OpCodes.Ldloca_S, lazy));

        //MethodReference tryGetValueMethod = ModuleDefinition.ImportReference(
        //        typeof(Dictionary<Type, Lazy<object>>).GetMethod(nameof(Dictionary<Type, Lazy<object>>.TryGetValue)));
        //body.Append(Instruction.Create(OpCodes.Callvirt, tryGetValueMethod));
        //body.Append(Instruction.Create(OpCodes.Brtrue_S, loadLazyInstruction));
        //body.Append(Instruction.Create(OpCodes.Ldloca_S, genericVariable));
        //body.Append(Instruction.Create(OpCodes.Initobj, genericParameter));
        //body.Append(Instruction.Create(OpCodes.Ldloc_1));
        //body.Append(Instruction.Create(OpCodes.Br_S, returnInstruction));
        //body.Append(loadLazyInstruction);
        //MethodReference lazyValueMethod =
        //    ModuleDefinition.ImportReference(typeof(Lazy<object>).GetProperty(nameof(Lazy<object>.Value)).GetMethod);
        //body.Append(Instruction.Create(OpCodes.Callvirt, lazyValueMethod));
        //body.Append(Instruction.Create(OpCodes.Unbox_Any, genericParameter));
        body.Append(returnInstruction);

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
        //TODO: Make static
        MethodReference addSingleton =
            ModuleDefinition.ImportReference(typeof(InternalMap).GetMethod(nameof(InternalMap.AddSingleton)));
        MethodReference addLazySingleton =
            ModuleDefinition.ImportReference(typeof(InternalMap).GetMethod(nameof(InternalMap.AddLazySingleton)));
        MethodReference addWeakTransient =
            ModuleDefinition.ImportReference(typeof(InternalMap).GetMethod(nameof(InternalMap.AddWeakTransient)));
        MethodReference addTransient =
            ModuleDefinition.ImportReference(typeof(InternalMap).GetMethod(nameof(InternalMap.AddTransient)));

        MethodReference funcCtor = ModuleDefinition.ImportReference(typeof(Func<object>).GetConstructors().Single());
        TypeReference type = ModuleDefinition.ImportReference(typeof(Type));
        MethodReference getTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

        int delegateMethodCount = 0;

        foreach (TypeMap map in mapping)
        {
            var delegateMethod = new MethodDefinition($"<{map.TargetType.Name}>_generated_{delegateMethodCount++}", MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Private, ModuleDefinition.Get<object>());
            ILProcessor delegateProcessor = delegateMethod.Body.GetILProcessor();
            bool foundCtor = false;
            foreach (MethodDefinition targetTypeCtor in map.TargetType.GetConstructors().OrderByDescending(c => c.Parameters.Count))
            {
                if (targetTypeCtor.Parameters.All(pd => pd.HasDefault && pd.Constant == null))
                {
                    foundCtor = true;
                    for (int i = 0; i < targetTypeCtor.Parameters.Count; i++)
                    {
                        delegateProcessor.Emit(OpCodes.Ldnull);
                    }
                    delegateProcessor.Emit(OpCodes.Newobj, targetTypeCtor);
                    break;
                }
            }
            if (!foundCtor)
            {
                LogWarning($"Could not find acceptable constructor for '{map.TargetType.FullName}'");
                continue;
            }
            delegateProcessor.Emit(OpCodes.Ret);
            delegateContainer.Methods.Add(delegateMethod);

            staticBody.Emit(OpCodes.Ldsfld, mapField);
            staticBody.Emit(OpCodes.Ldnull);
            staticBody.Emit(OpCodes.Ldftn, delegateMethod);
            staticBody.Emit(OpCodes.Newobj, funcCtor);

            staticBody.Emit(OpCodes.Ldc_I4, map.Keys.Count);
            staticBody.Emit(OpCodes.Newarr, type);

            int arrayIndex = 0;
            foreach(var key in map.Keys)
            {
                LogInfo($"  Mapping {key.FullName} -> {map.TargetType.FullName}");
                staticBody.Emit(OpCodes.Dup);
                staticBody.Emit(OpCodes.Ldc_I4, arrayIndex++);
                staticBody.Emit(OpCodes.Ldtoken, key);
                staticBody.Emit(OpCodes.Call, getTypeMethod);
                staticBody.Emit(OpCodes.Stelem_Ref);
            }

            var addMethod = new GenericInstanceMethod(addLazySingleton);
            addMethod.GenericArguments.Add(map.TargetType);

            staticBody.Emit(OpCodes.Call, addMethod);
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

    private class TypeMap
    {
        public TypeMap(TypeDefinition targetType)
        {
            TargetType = targetType;
        }

        public Create CreateType { get; } = Create.LazySingleton;

        public TypeDefinition TargetType { get; }

        public ICollection<TypeDefinition> Keys { get; } = new HashSet<TypeDefinition>(TypeDefinitionComparer.FullName);
    }

    private class Mapping : IEnumerable<TypeMap>
    {
        private readonly Dictionary<string, TypeMap> _maps = new Dictionary<string, TypeMap>();

        public void Add(TypeDefinition key, TypeDefinition targetType)
        {
            if (!_maps.TryGetValue(targetType.FullName, out TypeMap typeMap))
            {
                _maps[targetType.FullName] = typeMap = new TypeMap(targetType);
            }
            typeMap.Keys.Add(key);
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
