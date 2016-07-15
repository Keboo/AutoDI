using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Xml.Linq;
using AutoDI;
using Microsoft.Build.Framework;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
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
        foreach ( TypeDefinition type in ModuleDefinition.Types )
        {
            foreach ( var ctor in type.Methods.Where( x => x.IsConstructor ) )
            {
                var dependencyParameters = ctor.Parameters.Where(
                        p => p.CustomAttributes.Any( a => a.AttributeType.FullName == typeof( DependencyAttribute ).FullName ) ).ToList();

                if ( dependencyParameters.Any() )
                {
                    var resolverType = ModuleDefinition.ImportReference( typeof( IDependencyResolver ) );
                    var dependencyResolverType = ModuleDefinition.ImportReference( typeof( DependencyResolver ) );
                    
                    var instructions = ctor.Body.Instructions;
                    bool seenBaseCall = false;
                    int insertionPoint = instructions.IndexOf( instructions.SkipWhile( x =>
                     {
                         if ( x.OpCode != OpCodes.Nop && seenBaseCall )
                             return false;
                         if ( x.OpCode == OpCodes.Call )
                             seenBaseCall = true;
                         return true;
                     } ).First() );
                    
                    var end = Instruction.Create( OpCodes.Nop );

                    var resolverVariable = new VariableDefinition( resolverType );
                    ctor.Body.Variables.Add( resolverVariable );

                    instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Call,
                        new MethodReference( nameof( DependencyResolver.Get ), resolverType, dependencyResolverType ) ) );
                    //Store the resolver in our local variable
                    instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Stloc, resolverVariable ) );
                    //Push the resolver on top of the stack
                    instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Ldloc, resolverVariable ) );
                    //Branch to the end if the resolver is null
                    instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Brfalse, end ) );

                    foreach ( ParameterDefinition parameter in dependencyParameters )
                    {
                        TypeReference parameterType = ModuleDefinition.ImportReference( parameter.ParameterType );
                        var afterParam = Instruction.Create( OpCodes.Nop );
                        //Push dependency parameter onto the stack
                        instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Ldarg, parameter ) );
                        //Push null onto the stack - TODO check type and push default value?
                        instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Ldnull ) );
                        //Push 1 if the values are equal, 0 if they are not equal
                        instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Ceq ) );
                        //Branch if the value is false (0), the dependency was set by the caller we wont replace it
                        instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Brfalse_S, afterParam ) );
                        //Push the dependency resolver onto the stack
                        instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Ldloc, resolverVariable ) );
                        //Call the resolve method
                        var resolveMethod = ModuleDefinition.ImportReference(typeof(IDependencyResolver).GetMethod(nameof(IDependencyResolver.Resolve)));
                        resolveMethod = new GenericInstanceMethod(resolveMethod){ GenericArguments = { parameterType }};
                        instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Callvirt, resolveMethod ) );
                        //Store the return from the resolve method in the method parameter
                        instructions.Insert( insertionPoint++, Instruction.Create( OpCodes.Starg, parameter ) );
                        instructions.Insert( insertionPoint++, afterParam );
                    }
                    instructions.Insert( insertionPoint, end );
                    ctor.Body.OptimizeMacros();
                }
            }
        }
        ModuleDefinition.Write( AssemblyFilePath );
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

