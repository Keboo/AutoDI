
using System.Linq;
using AutoDI;
using AutoDI.Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

// ReSharper disable once CheckNamespace
partial class ModuleWeaver
{
    private void InjectContainer(TypeDefinition resolverType)
    {
        if (ModuleDefinition.EntryPoint != null)
        {
            //ILProcessor entryMethodProcessor = ModuleDefinition.EntryPoint.Body.GetILProcessor();
            //var create = Instruction.Create(OpCodes.Newobj,
            //    resolverType.Methods.Single(m => m.IsConstructor && !m.IsStatic));
            //var setMethod = ModuleDefinition.ImportReference(typeof(DependencyResolver).GetMethod(
            //    nameof(DependencyResolver.Set),
            //    new[] { typeof(IDependencyResolver) }));
            //var set = Instruction.Create(OpCodes.Call, setMethod);
            //entryMethodProcessor.InsertBefore(ModuleDefinition.EntryPoint.Body.Instructions.First(), set);
            //entryMethodProcessor.InsertBefore(set, create);
        }
        else
        {
            InternalLogDebug($"No entry point in {ModuleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
        }
    }
}