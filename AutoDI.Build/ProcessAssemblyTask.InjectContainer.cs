using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoDI.Build
{
    partial class ProcessAssemblyTask
    {
        private void InjectInitCall(MethodReference initMethod)
        {
            if (ModuleDefinition.EntryPoint != null)
            {
                Logger.Debug("Injecting AutoDI init call", DebugLogLevel.Verbose);

                var injector = new Injector(ModuleDefinition.EntryPoint);
                injector.Insert(OpCodes.Ldnull);
                injector.Insert(OpCodes.Call, initMethod);
            }
            else
            {
                Logger.Debug($"No entry point in {ModuleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
            }
        }

        private void InjectModuleCctorInitCall(MethodReference initMethod)
        {
            var moduleClass = ModuleDefinition.Types.FirstOrDefault(t => t.Name == "<Module>");
            if (moduleClass == null)
            {
                Logger.Debug($"No module class in {ModuleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
                return;
            }

            var cctor = FindOrCreateCctor(moduleClass);    
            if (cctor != null)
            {
                Logger.Debug("Injecting AutoDI .cctor init call", DebugLogLevel.Verbose);

                var injector = new Injector(cctor);
                injector.Insert(OpCodes.Ldnull);
                injector.Insert(OpCodes.Call, initMethod);
            }
            else
            {
                Logger.Debug($"Couldn't find or create .cctor in {ModuleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
            }

        }

        private MethodDefinition FindOrCreateCctor(TypeDefinition moduleClass)
        {
            var cctor = moduleClass.Methods.FirstOrDefault(m => m.Name == ".cctor");
            if (cctor == null)
            {
                var attributes = MethodAttributes.Private
                    | MethodAttributes.HideBySig
                    | MethodAttributes.Static
                    | MethodAttributes.SpecialName
                    | MethodAttributes.RTSpecialName;
                cctor = new MethodDefinition(".cctor", attributes, ModuleDefinition.ImportReference(typeof(void)));
                moduleClass.Methods.Add(cctor);
                cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            return cctor;
        }
    }
}