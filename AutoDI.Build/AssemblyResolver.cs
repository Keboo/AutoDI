using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace AutoDI.Build
{
    internal class AssemblyResolver : BaseAssemblyResolver, ITypeResolver
    {
        private readonly ILogger _logger;
        private readonly IDictionary<string, AssemblyDefinition> _assemblyCache;
        private readonly IDictionary<string, TypeDefinition> _typeCache;

        public AssemblyResolver(IEnumerable<string> assembliesToInclude, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _assemblyCache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
            _typeCache = new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);
            AddSearchDirectory(Path.GetFullPath("."));
            AddSearchDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            ResolveFailure += OnResolveFailure;
            foreach (var assemblyPath in assembliesToInclude)
            {
                AssemblyDefinition assembly = GetFromPath(assemblyPath);
                if (assembly != null)
                {
                    _assemblyCache[assembly.Name.Name] = assembly;
                }
            }
            logger.Info("Done loading referenced assemblies");
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters readParameters)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (_assemblyCache.TryGetValue(name.Name, out AssemblyDefinition assemblyDefinition))
            {
                return assemblyDefinition;
            }
            assemblyDefinition = base.Resolve(name, readParameters);
            
            if (assemblyDefinition != null)
            {
                _assemblyCache[name.Name] = assemblyDefinition;
            }
            else
            {
                _logger.Info($"Could not find {name.Name}");
            }
            return assemblyDefinition;
        }

        public TypeDefinition ResolveType(string fullTypeName)
        {
            if (fullTypeName == null) throw new ArgumentNullException(nameof(fullTypeName));
            if (_typeCache.TryGetValue(fullTypeName, out TypeDefinition type))
            {
                return type;
            }

            foreach (AssemblyDefinition assembly in _assemblyCache.Values)
            {
                type = assembly.MainModule.GetType(fullTypeName);
                if (type != null)
                {
                    return _typeCache[fullTypeName] = type;
                }
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (AssemblyDefinition assemblyDefinition in _assemblyCache.Values)
            {
                assemblyDefinition?.Dispose();
            }
            _assemblyCache.Clear();
            base.Dispose(disposing);
        }

        private AssemblyDefinition OnResolveFailure(object sender, AssemblyNameReference reference)
        {
            Assembly assembly;
            try
            {
#pragma warning disable 618
                assembly = Assembly.LoadWithPartialName(reference.Name);
#pragma warning restore 618
            }
            catch (FileNotFoundException)
            {
                assembly = null;
            }

            if (!string.IsNullOrWhiteSpace(assembly?.CodeBase))
            {
                string path = new Uri(assembly.CodeBase).AbsolutePath;
                return GetFromPath(path);
            }

            return null;
        }

        private AssemblyDefinition GetFromPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return null;
            }
            var readerParameters = new ReaderParameters(ReadingMode.Deferred)
            {
                ReadWrite = false,
                ReadSymbols = false,
                AssemblyResolver = this
            };
            return AssemblyDefinition.ReadAssembly(filePath, readerParameters);
        }
    }
}