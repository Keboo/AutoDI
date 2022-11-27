using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

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
        if (Logger is null)
        {
            Logger = new TaskLogger(this);
        }

        Logger.Info($"Starting AutoDI on '{AssemblyFile}'");
        var sw = Stopwatch.StartNew();

        using (var assemblyResolver = new AssemblyResolver(GetIncludedReferences(), Logger))
        {
            if (AssemblyResolver is null)
            {
                AssemblyResolver = assemblyResolver;
            }

            if (TypeResolver is null)
            {
                TypeResolver = assemblyResolver;
            }

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

    private Stream? GetSymbolInformation(
        out ISymbolReaderProvider? symbolReaderProvider,
        out ISymbolWriterProvider? symbolWriterProvider)
    {
        if (string.Equals("none", DebugType, StringComparison.OrdinalIgnoreCase))
        {
            Logger?.Info("No symbols");
            symbolReaderProvider = null;
            symbolWriterProvider = null;
            return null;
        }

        if (string.Equals("embedded", DebugType, StringComparison.OrdinalIgnoreCase))
        {
            Logger?.Info("Using embedded symbols");
            symbolReaderProvider = new EmbeddedPortablePdbReaderProvider();
            symbolWriterProvider = new EmbeddedPortablePdbWriterProvider();
            return null;
        }

        string? pdbPath = FindPdbPath();
        string? mdbPath = FindMdbPath();

        if (pdbPath != null && mdbPath != null)
        {
            if (File.GetLastWriteTimeUtc(pdbPath) >= File.GetLastWriteTimeUtc(mdbPath))
            {
                mdbPath = null;
                Logger?.Debug("Found mdb and pdb debug symbols. Selected pdb (newer).", DebugLogLevel.Verbose);
            }
            else
            {
                pdbPath = null;
                Logger?.Debug("Found mdb and pdb debug symbols. Selected mdb (newer).", DebugLogLevel.Verbose);
            }
        }

        if (pdbPath != null)
        {
            if (IsPortablePdb(pdbPath))
            {
                Logger?.Info($"Using portable symbol file {pdbPath}");
                symbolReaderProvider = new PortablePdbReaderProvider();
                symbolWriterProvider = new PortablePdbWriterProvider();
            }
            else
            {
                Logger?.Info($"Using symbol file {pdbPath}");
                symbolReaderProvider = new PdbReaderProvider();
                symbolWriterProvider = new PdbWriterProvider();
            }
            string tempPath = pdbPath + ".tmp";
            File.Copy(pdbPath, tempPath, true);
            return new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        else if (mdbPath != null)
        {
            Logger?.Info($"Using symbol file {mdbPath}");
            symbolReaderProvider = new MdbReaderProvider();
            symbolWriterProvider = new MdbWriterProvider();
            string tempPath = mdbPath + ".tmp";
            File.Copy(mdbPath, tempPath, true);
            return new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        symbolReaderProvider = null;
        symbolWriterProvider = null;
        return null;

        string? FindPdbPath()
        {
            // because UWP use a wacky convention for symbols
            string path = Path.ChangeExtension(AssemblyFile, "compile.pdb");
            if (File.Exists(path))
            {
                Logger?.Debug($"Found debug symbols at '{path}'.", DebugLogLevel.Verbose);
                return path;
            }
            path = Path.ChangeExtension(AssemblyFile, "pdb");
            if (File.Exists(path))
            {
                Logger?.Debug($"Found debug symbols at '{path}'.", DebugLogLevel.Verbose);
                return path;
            }
            return null;
        }

        bool IsPortablePdb(string symbolsPath)
        {
            using var fileStream = File.OpenRead(symbolsPath);
            using var reader = new BinaryReader(fileStream);
            return reader.ReadBytes(4).SequenceEqual(new byte[] { 0x42, 0x4a, 0x53, 0x42 });
        }

        string? FindMdbPath()
        {
            string path = AssemblyFile + ".mdb";
            if (File.Exists(path))
            {
                Logger?.Debug($"Found debug symbols at '{path}'.", DebugLogLevel.Verbose);
                return path;
            }
            return null;
        }
    }

    protected abstract bool WeaveAssembly();
}