using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System.Xml.Linq;

namespace AutoDI.Build
{
    public abstract class AssemblyRewriteTask : Task, ICancelableTask
    {
        [Required]
        public string AssemblyFile { set; get; }

        //[Required]
        //public string IntermediateDirectory { get; set; }

        protected ILogger Logger { get; set; }

        protected IAssemblyResolver AssemblyResolver { get; set; }

        protected ITypeResolver TypeResolver { get; set; }

        protected ModuleDefinition ModuleDefinition { get; private set; }

        protected AssemblyDefinition ResolveAssembly(string assemblyName)
        {
            return AssemblyResolver.Resolve(AssemblyNameReference.Parse(assemblyName));
        }

        public XElement Config { get; set; }

        public override bool Execute()
        {
            if (Logger == null)
            {
                Logger = new TaskLogger(this);
            }

            var assemblyResolver = new AssemblyResolver();
            if (AssemblyResolver == null)
            {
                AssemblyResolver = assemblyResolver;
            }
            if (TypeResolver == null)
            {
                TypeResolver = assemblyResolver;
            }

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = AssemblyResolver,
                //ReadSymbols = SymbolStream != null || DebugSymbols == DebugSymbolsType.Embedded,
                //SymbolReaderProvider = debugReaderProvider,
                //SymbolStream = SymbolStream,
                InMemory = true
            };

            foreach (var assemblyName in GetAssembliesToInclude())
            {
                AssemblyResolver.Resolve(new AssemblyNameReference(assemblyName, null));
            }

            using (ModuleDefinition = ModuleDefinition.ReadModule(AssemblyFile, readerParameters))
            {
                if (WeaveAssembly())
                {
                    var parameters = new WriterParameters
                    {
                        //StrongNameKeyPair = StrongNameKeyPair,
                        //WriteSymbols = debugWriterProvider != null,
                        //SymbolWriterProvider = debugWriterProvider,
                    };

                    //ModuleDefinition.Assembly.Name.PublicKey = PublicKey;
                    ModuleDefinition.Write(AssemblyFile, parameters);
                }
            }

            return !Logger.ErrorLogged;
        }

        public void Cancel()
        {
            
        }

        protected virtual IEnumerable<string> GetAssembliesToInclude()
        {
            yield return "mscorlib";
            yield return "System";
            yield return "System.Runtime";
            yield return "System.Core";
            yield return "netstandard";
            yield return "AutoDI";
            yield return "Microsoft.Extensions.DependencyInjection.Abstractions";
            yield return "System.Collections";
            yield return "System.ObjectModel";
            yield return "System.Threading";
        }

        protected abstract bool WeaveAssembly();
    }
}