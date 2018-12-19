using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AutoDI.Build
{
    public abstract class AssemblyRewriteTask : Task, ICancelableTask
    {
        [Required]
        public string AssemblyFile { set; get; }

        [Required]
        public string References { get; set; }

        [Required]
        public string DebugType { get; set; }

        //[Required]
        //public string IntermediateDirectory { get; set; }

        protected ILogger Logger { get; set; }

        protected IAssemblyResolver AssemblyResolver { get; set; }

        protected ITypeResolver TypeResolver { get; set; }

        protected ModuleDefinition ModuleDefinition { get; private set; }

        protected AssemblyDefinition ResolveAssembly(string assemblyName)
        {
            return AssemblyResolver?.Resolve(AssemblyNameReference.Parse(assemblyName));
        }

        public override bool Execute()
        {
            if (Logger == null)
            {
                Logger = new TaskLogger(this);
            }

            Logger.Info("Starting AutoDI");
            var sw = Stopwatch.StartNew();

            using (var assemblyResolver = new AssemblyResolver(GetIncludedReferences(), Logger))
            {
                if (AssemblyResolver == null)
                {
                    AssemblyResolver = assemblyResolver;
                }

                if (TypeResolver == null)
                {
                    TypeResolver = assemblyResolver;
                }

                foreach (var assemblyName in GetAssembliesToInclude())
                {
                    AssemblyResolver.Resolve(new AssemblyNameReference(assemblyName, null));
                }

                using (Stream symbolStream = GetSymbolInformation(
                    out ISymbolReaderProvider symbolReaderProvider,
                    out ISymbolWriterProvider symbolWriterProvider))
                {
                    var readerParameters = new ReaderParameters
                    {
                        AssemblyResolver = AssemblyResolver,

                        ReadSymbols = symbolStream != null || symbolReaderProvider is EmbeddedPortablePdbReaderProvider,
                        SymbolReaderProvider = symbolReaderProvider,
                        SymbolStream = symbolStream,
                        InMemory = true
                    };
                    
                    using (ModuleDefinition = ModuleDefinition.ReadModule(AssemblyFile, readerParameters))
                    {
                        Logger.Info($"Loaded '{AssemblyFile}'");
                        if (WeaveAssembly())
                        {
                            Logger.Info("Weaving complete - updating assembly");
                            var parameters = new WriterParameters
                            {
                                WriteSymbols = symbolReaderProvider != null,
                                SymbolWriterProvider = symbolWriterProvider,
                            };

                            ModuleDefinition.Write(AssemblyFile, parameters);
                        }
                        else
                        {
                            Logger.Info("Weaving complete - no update");
                        }
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

        private Stream GetSymbolInformation(out ISymbolReaderProvider symbolReaderProvider,
            out ISymbolWriterProvider symbolWriterProvider)
        {
            if (string.Equals("none", DebugType, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("No symbols");
                symbolReaderProvider = null;
                symbolWriterProvider = null;
                return null;
            }

            if (string.Equals("embedded", DebugType, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("Using embedded symbols");
                symbolReaderProvider = new EmbeddedPortablePdbReaderProvider();
                symbolWriterProvider = new EmbeddedPortablePdbWriterProvider();
                return null;
            }

            string pdbPath = FindPdbPath();
            string mdbPath = FindMdbPath();

            if (pdbPath != null && mdbPath != null)
            {
                if (File.GetLastWriteTimeUtc(pdbPath) >= File.GetLastWriteTimeUtc(mdbPath))
                {
                    mdbPath = null;
                    Logger.Debug("Found mdb and pdb debug symbols. Selected pdb (newer).", DebugLogLevel.Verbose);
                }
                else
                {
                    pdbPath = null;
                    Logger.Debug("Found mdb and pdb debug symbols. Selected mdb (newer).", DebugLogLevel.Verbose);
                }
            }

            if (pdbPath != null)
            {
                Logger.Info($"Using symbol file {pdbPath}");
                symbolReaderProvider = new PdbReaderProvider();
                symbolWriterProvider = new PdbWriterProvider();
                string tempPath = pdbPath + ".tmp";
                File.Copy(pdbPath, tempPath, true);
                return new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            else if (mdbPath != null)
            {
                Logger.Info($"Using symbol file {mdbPath}");
                symbolReaderProvider = new MdbReaderProvider();
                symbolWriterProvider = new MdbWriterProvider();
                string tempPath = mdbPath + ".tmp";
                File.Copy(mdbPath, tempPath, true);
                return new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }

            symbolReaderProvider = null;
            symbolWriterProvider = null;
            return null;

            string FindPdbPath()
            {
                // because UWP use a wacky convention for symbols
                string path = Path.ChangeExtension(AssemblyFile, "compile.pdb");
                if (File.Exists(path))
                {
                    Logger.Debug($"Found debug symbols at '{path}'.", DebugLogLevel.Verbose);
                    return path;
                }
                path = Path.ChangeExtension(AssemblyFile, "pdb");
                if (File.Exists(path))
                {
                    Logger.Debug($"Found debug symbols at '{path}'.", DebugLogLevel.Verbose);
                    return path;
                }
                return null;
            }

            string FindMdbPath()
            {
                string path = AssemblyFile + ".mdb";
                if (File.Exists(path))
                {
                    Logger.Debug($"Found debug symbols at '{path}'.", DebugLogLevel.Verbose);
                    return path;
                }
                return null;
            }
        }

        protected abstract bool WeaveAssembly();
    }
}