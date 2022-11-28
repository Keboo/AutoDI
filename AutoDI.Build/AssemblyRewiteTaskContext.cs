using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoDI.Build;

public class AssemblyRewiteTaskContext : ILogger
{
    public ModuleDefinition ModuleDefinition { get; }
    public ITypeResolver TypeResolver { get; }
    public IAssemblyResolver AssemblyResolver { get; }
    private ILogger Logger { get; }

    public bool ErrorLogged => Logger.ErrorLogged;

    public DebugLogLevel DebugLogLevel 
    {
        get => Logger.DebugLogLevel;
        set => Logger.DebugLogLevel = value;
    }

    public AssemblyRewiteTaskContext(
        ModuleDefinition moduleDefinition,
        ITypeResolver typeResolver,
        IAssemblyResolver assemblyResolver,
        ILogger logger)
    {
        ModuleDefinition = moduleDefinition;
        TypeResolver = typeResolver;
        AssemblyResolver = assemblyResolver;
        Logger = logger;
    }

    public AssemblyDefinition? ResolveAssembly(string assemblyName)
    {
        return AssemblyResolver.Resolve(AssemblyNameReference.Parse(assemblyName));
    }
    
    public void Debug(string message, DebugLogLevel debugLevel) 
        => Logger.Debug(message, debugLevel);

    public void Info(string message) 
        => Logger.Info(message);

    public void Warning(string message, SequencePoint? sequencePoint = null) 
        => Logger.Warning(message, sequencePoint);

    public void Error(string message, SequencePoint? sequencePoint) 
        => Logger.Error(message, sequencePoint);
}
