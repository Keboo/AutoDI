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

    public override bool Execute()
    {
        //Debugger.Launch();
        var logger = new TaskLogger(this);

        logger.Info($"Starting AutoDI on '{AssemblyFile}'");
        var sw = Stopwatch.StartNew();

        using (var assemblyResolver = new AssemblyResolver(GetIncludedReferences(), logger))
        {
            foreach (var assemblyName in GetAssembliesToInclude())
            {
                assemblyResolver.Resolve(new AssemblyNameReference(assemblyName, null));
            }

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = assemblyResolver,
                InMemory = true
            };

            using var moduleDefinition = ModuleDefinition.ReadModule(AssemblyFile, readerParameters);
            bool loadedSymbols;
            try
            {
                moduleDefinition.ReadSymbols();
                loadedSymbols = true;
            }
            catch
            {
                loadedSymbols = false;
            }
            logger.Info($"Loaded '{AssemblyFile}'");
            AssemblyRewiteTaskContext context = new(moduleDefinition, assemblyResolver, assemblyResolver, logger);
            if (WeaveAssembly(context))
            {
                logger.Info("Weaving complete - updating assembly");
                var parameters = new WriterParameters
                {
                    WriteSymbols = loadedSymbols,
                };

                moduleDefinition.Write(AssemblyFile, parameters);
            }
            else
            {
                logger.Info("Weaving complete - no update");
            }
        }

        sw.Stop();
        logger.Info($"AutoDI Complete {sw.Elapsed}");
        return !logger.ErrorLogged;
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

    protected abstract bool WeaveAssembly(AssemblyRewiteTaskContext context);
}