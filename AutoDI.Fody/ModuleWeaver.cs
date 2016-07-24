using AutoDI;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AutoDI.Fody;
using OpCodes = Mono.Cecil.Cil.OpCodes;

// ReSharper disable once CheckNamespace
public class ModuleWeaver
{
    // Will contain the full element XML from FodyWeavers.xml. OPTIONAL
    public XElement Config { get; set; }

    // Will log an MessageImportance.Normal message to MSBuild. OPTIONAL
    public Action<string> LogDebug { get; set; }

    // Will log an MessageImportance.High message to MSBuild. OPTIONAL
    public Action<string> LogInfo { get; set; }

    // Will log a message to MSBuild. OPTIONAL
    public Action<string, MessageImportance> LogMessage { get; set; }

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
            foreach (TypeDefinition type in ModuleDefinition.Types)
            {
                foreach (MethodDefinition ctor in type.Methods.Where(x => x.IsConstructor))
                {
                    ProcessConstructor(type, ctor);
                }
            }
            ModuleDefinition.Write(AssemblyFilePath);
        }
        catch (Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception e = ex; e != null; e = e.InnerException)
                sb.AppendLine(e.ToString());
            LogError(sb.ToString());
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
                if ((values?.Length ?? 0) == 0)
                {
                    //Push empty array onto the stack
                    injector.Insert(OpCodes.Call, new GenericInstanceMethod(
                        ModuleDefinition.ImportReference(typeof(Array).GetMethod(nameof(Array.Empty))))
                    {
                        GenericArguments = { ModuleDefinition.ImportReference(typeof(object)) }
                    });
                }
                else
                {
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

