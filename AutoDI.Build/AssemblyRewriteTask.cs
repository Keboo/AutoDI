using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Mono.Cecil;

namespace AutoDI.Build;

public abstract class AssemblyRewriteTask : Task, ICancelableTask
{
    [Required]
    public string AssemblyFile { set; get; } = "";

    [Required]
    public string References { get; set; } = "";

    [Required]
    public string DebugType { get; set; } = "";

    protected ILogger? Logger { get; set; }

    protected IAssemblyResolver? AssemblyResolver { get; set; }

    protected ITypeResolver? TypeResolver { get; set; }

    protected ModuleDefinition? ModuleDefinition { get; private set; }

    protected AssemblyDefinition? ResolveAssembly(string assemblyName)
    {
        return AssemblyResolver?.Resolve(AssemblyNameReference.Parse(assemblyName));
    }

    public override bool Execute()
    {
        //Debugger.Launch();
        Logger ??= new TaskLogger(this);

        Logger.Info($"Starting AutoDI on '{AssemblyFile}'");
        var sw = Stopwatch.StartNew();

        using (var assemblyResolver = new AssemblyResolver(GetIncludedReferences(), Logger))
        {
            AssemblyResolver ??= assemblyResolver;

            TypeResolver ??= assemblyResolver;

            foreach (var assemblyName in GetAssembliesToInclude())
            {
                AssemblyResolver.Resolve(new AssemblyNameReference(assemblyName, null));
            }

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = AssemblyResolver,
                InMemory = true
            };

            using (ModuleDefinition = ModuleDefinition.ReadModule(AssemblyFile, readerParameters))
            {
                bool loadedSymbols;
                try
                {
                    ModuleDefinition.ReadSymbols();
                    loadedSymbols = true;
                }
                catch
                {
                    loadedSymbols = false;
                }
                Logger.Info($"Loaded '{AssemblyFile}'");
                if (WeaveAssembly())
                {
                    Logger.Info("Weaving complete - updating assembly");
                    var parameters = new WriterParameters
                    {
                        WriteSymbols = loadedSymbols,
                    };

                    ModuleDefinition.Write(AssemblyFile, parameters);
                }
                else
                {
                    Logger.Info("Weaving complete - no update");
                }
            }
        }

        sw.Stop();
        Logger.Info($"AutoDI Complete {sw.Elapsed}");
        return !Logger.ErrorLogged;
    }

    public virtual void Cancel()
    {

    }

    private IEnumerable<string> GetIncludedReferences()
    {
        if (!string.IsNullOrWhiteSpace(References))
        {
            foreach (var reference in References.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                yield return reference;
            }
        }
    }

    protected virtual IEnumerable<string> GetAssembliesToInclude()
    {
        yield return "mscorlib";
        yield return "System";
        yield return "netstandard";
        yield return "System.Collections";
    }

    protected abstract bool WeaveAssembly();
}