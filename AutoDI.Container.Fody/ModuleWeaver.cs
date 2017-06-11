using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AutoDI;
using AutoDI.Container.Fody;
using Mono.Cecil.Rocks;

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
    }

    public void Execute()
    {
        try
        {
            var mapping = new Dictionary<TypeDefinition, TypeDefinition>();
            mapping[ModuleDefinition.GetType("AutoDI.Container.Test", "IManager")] =
                ModuleDefinition.GetType("AutoDI.Container.Test", "Manager");

            TypeDefinition resolverType = CreateAutoDIContainer(mapping);
            ModuleDefinition.Types.Add(resolverType);

            return;
            var typesByInterface = new Dictionary<TypeDefinition, TypeDefinition>();
            foreach (TypeDefinition type in ModuleDefinition.Types)
            {
                if (type.IsClass && type.HasInterfaces)
                {
                    foreach (var @interface in type.Interfaces)
                    {

                    }
                }
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

    private TypeDefinition CreateAutoDIContainer(IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> mapping)
    {
        var type = new TypeDefinition("AutoDI", "AutoDIContainer",
            TypeAttributes.Class | TypeAttributes.Public)
        {
            BaseType = ModuleDefinition.Get<object>()
        };

        
        //Create delegate container class
        var nestedType = new TypeDefinition("AutoDI", "AutoDIContainer.<>c",
            TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.Serializable |
            TypeAttributes.BeforeFieldInit);

        FieldDefinition delegateInstanceField = CreateStaticReadonlyField("_instance", true, nestedType);
        nestedType.Fields.Add(delegateInstanceField);

        MethodDefinition nestedTypeCtor = CreateConstructor();
        nestedType.Methods.Add(nestedTypeCtor);

        MethodDefinition nestedTypeStaticCtor = CreateStaticConstructor();
        ILProcessor nestedStaticBody = nestedTypeStaticCtor.Body.GetILProcessor();
        nestedStaticBody.Emit(OpCodes.Newobj, nestedTypeCtor);
        nestedStaticBody.Emit(OpCodes.Stsfld, delegateInstanceField);
        nestedStaticBody.Emit(OpCodes.Ret);
        nestedType.Methods.Add(nestedTypeStaticCtor);

        type.NestedTypes.Add(nestedType);

        //Declare dictionary map
        FieldDefinition mapField = CreateStaticReadonlyField<Dictionary<Type, Lazy<object>>>("_items", false);
        type.Fields.Add(mapField);

        //Create static constructor
        MethodDefinition staticConstructor = CreateStaticConstructor();
        ILProcessor staticBody = staticConstructor.Body.GetILProcessor();

        MethodReference dictionaryConstructor = ModuleDefinition.ImportReference(typeof(Dictionary<Type, Lazy<object>>).GetConstructor(new Type[0]));
        staticBody.Emit(OpCodes.Newobj, dictionaryConstructor);
        staticBody.Emit(OpCodes.Stsfld, mapField);

        BuildMap(mapField, staticBody, nestedType, mapping);

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
        body.Append(Instruction.Create(OpCodes.Ldtoken, genericParameter));
        MethodReference getTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
        body.Append(Instruction.Create(OpCodes.Call, getTypeMethod));
        body.Append(Instruction.Create(OpCodes.Ldloca_S, lazy));
        
        MethodReference tryGetValueMethod = ModuleDefinition.ImportReference(
                typeof(Dictionary<Type, Lazy<object>>).GetMethod(nameof(Dictionary<Type, Lazy<object>>.TryGetValue)));
        body.Append(Instruction.Create(OpCodes.Callvirt, tryGetValueMethod));
        body.Append(Instruction.Create(OpCodes.Brtrue_S, loadLazyInstruction));
        body.Append(Instruction.Create(OpCodes.Ldloca_S, genericVariable));
        body.Append(Instruction.Create(OpCodes.Initobj, genericParameter));
        body.Append(Instruction.Create(OpCodes.Ldloc_1));
        body.Append(Instruction.Create(OpCodes.Br_S, returnInstruction));
        body.Append(loadLazyInstruction);
        MethodReference lazyValueMethod =
            ModuleDefinition.ImportReference(typeof(Lazy<object>).GetProperty(nameof(Lazy<object>.Value)).GetMethod);
        body.Append(Instruction.Create(OpCodes.Callvirt, lazyValueMethod));
        body.Append(Instruction.Create(OpCodes.Unbox_Any, genericParameter));
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

    private void BuildMap(FieldDefinition mapField, ILProcessor staticBody, TypeDefinition delegateContainer, IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> mapping)
    {
        //TODO: Make static
        MethodReference getTypeMethod = ModuleDefinition.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
        MethodReference funcCtor = ModuleDefinition.ImportReference(typeof(Func<object>).GetConstructors().Single());
        MethodReference lazyCtor = ModuleDefinition.ImportReference(typeof(Lazy<object>).GetConstructor(new[] {typeof(Func<object>)}));
        MethodReference dictionarySetItem = ModuleDefinition.ImportReference(typeof(Dictionary<Type, Lazy<object>>).GetMethod("set_Item"));
        int delegateMethodCount = 0;

        foreach (KeyValuePair<TypeDefinition, TypeDefinition> kvp in mapping)
        {
            staticBody.Emit(OpCodes.Ldsfld, mapField);
            staticBody.Emit(OpCodes.Ldtoken, kvp.Key);
            staticBody.Emit(OpCodes.Call, getTypeMethod);
            staticBody.Emit(OpCodes.Ldsfld, delegateContainer.Fields.Single());
      
            var delegateMethod = new MethodDefinition($"</cctor>b__1_{delegateMethodCount++}", MethodAttributes.Assembly | MethodAttributes.HideBySig, ModuleDefinition.Get<object>());
            delegateContainer.Methods.Add(delegateMethod);
            ILProcessor delegateProcessor = delegateMethod.Body.GetILProcessor();
            bool foundCtor = false;
            foreach (MethodDefinition targetTypeCtor in kvp.Value.GetConstructors().OrderByDescending(c => c.Parameters.Count))
            {
                if (targetTypeCtor.Parameters.All(pd => pd.HasDefault && pd.Constant == null))
                {
                    foundCtor = true;
                    for(int i = 0; i < targetTypeCtor.Parameters.Count; i++)
                        delegateProcessor.Emit(OpCodes.Ldnull);
                    delegateProcessor.Emit(OpCodes.Newobj, targetTypeCtor);
                    break;
                }
            }
            if (!foundCtor)
            {
                LogWarning($"Could not find acceptable constructor for '{kvp.Value.FullName}'");
            }
            delegateProcessor.Emit(OpCodes.Ret);
            staticBody.Emit(OpCodes.Ldftn, delegateMethod);
            staticBody.Emit(OpCodes.Newobj, funcCtor);
            staticBody.Emit(OpCodes.Newobj, lazyCtor);
            staticBody.Emit(OpCodes.Callvirt, dictionarySetItem);
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

    private FieldDefinition CreateStaticReadonlyField<T>(string name, bool @public )
    {
        return CreateStaticReadonlyField(name, @public, ModuleDefinition.Get<T>());
    }

    private FieldDefinition CreateStaticReadonlyField(string name, bool @public, TypeReference type)
    {
        return new FieldDefinition(name,
            (@public ? FieldAttributes.Public : FieldAttributes.Private) | FieldAttributes.Static |
            FieldAttributes.InitOnly, type);
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

public class AutoDIContainer : IDependencyResolver
{
    private static readonly Dictionary<Type, Lazy<object>> _objects = new Dictionary<Type, Lazy<object>>();

    static AutoDIContainer()
    {
        //_objects[typeof(object)] = new Lazy<object>(() => Get<object>());
    }

    T IDependencyResolver.Resolve<T>(params object[] parameters) => Get<T>();

    private static T Get<T>()
    {
        if (_objects.TryGetValue(typeof(T), out Lazy<object> lazy))
        {
            return (T)lazy.Value;
        }
        return default(T);
    }
}

