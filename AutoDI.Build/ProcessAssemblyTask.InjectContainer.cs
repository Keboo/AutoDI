using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoDI.Build;

partial class ProcessAssemblyTask
{
    private static void InjectInitCall(ModuleDefinition moduleDefinition, MethodReference initMethod, ILogger logger)
    {
        if (moduleDefinition.EntryPoint != null)
        {
            logger.Debug("Injecting AutoDI init call", DebugLogLevel.Verbose);

            var injector = new Injector(moduleDefinition.EntryPoint);
            injector.Insert(OpCodes.Ldnull);
            injector.Insert(OpCodes.Call, initMethod);
        }
        else
        {
            logger.Debug($"No entry point in {moduleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
        }
    }

    private static void InjectModuleCctorInitCall(ModuleDefinition moduleDefinition, MethodReference initMethod, ILogger logger)
    {
        var moduleClass = moduleDefinition.Types.FirstOrDefault(t => t.Name == "<Module>");
        if (moduleClass is null)
        {
            logger.Debug($"No module class in {moduleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
            return;
        }

        var cctor = FindOrCreateCctor(moduleDefinition, moduleClass);
        if (cctor is not null)
        {
            logger.Debug("Injecting AutoDI .cctor init call", DebugLogLevel.Verbose);

            var injector = new Injector(cctor);
            injector.Insert(OpCodes.Ldnull);
            injector.Insert(OpCodes.Call, initMethod);
        }
        else
        {
            logger.Debug($"Couldn't find or create .cctor in {moduleDefinition.FileName}. Skipping container injection.", DebugLogLevel.Default);
        }

    }

    private static MethodDefinition FindOrCreateCctor(ModuleDefinition moduleDefinition, TypeDefinition moduleClass)
    {
        var cctor = moduleClass.Methods.FirstOrDefault(m => m.Name == ".cctor");
        if (cctor is null)
        {
            var attributes = MethodAttributes.Private
                | MethodAttributes.HideBySig
                | MethodAttributes.Static
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName;
            cctor = new MethodDefinition(".cctor", attributes, moduleDefinition.ImportReference(typeof(void)));
            moduleClass.Methods.Add(cctor);
            cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        return cctor;
    }
}