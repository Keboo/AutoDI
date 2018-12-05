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
    }
}