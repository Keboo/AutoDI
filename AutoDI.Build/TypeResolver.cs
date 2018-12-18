using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoDI.Build
{
    internal class TypeResolver
    {
        private readonly ModuleDefinition _module;
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly ILogger _logger;

        public TypeResolver(ModuleDefinition module, IAssemblyResolver assemblyResolver,
            ILogger logger)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _assemblyResolver = assemblyResolver ?? throw new ArgumentNullException(nameof(assemblyResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ICollection<TypeDefinition> GetAllTypes(Settings settings, out AssemblyDefinition autoDIAssembly)
        {
            autoDIAssembly = null;
            var allTypes = new HashSet<TypeDefinition>(TypeComparer.FullName);
            IEnumerable<TypeDefinition> FilterTypes(IEnumerable<TypeDefinition> types) =>
                types.Where(t => !t.IsCompilerGenerated() && !allTypes.Remove(t));

            const string autoDIFullName = "AutoDI";
            foreach (ModuleDefinition module in GetAllModules())
            {
                if (module.Assembly.Name.Name == autoDIFullName)
                {
                    autoDIAssembly = _assemblyResolver.Resolve(module.Assembly.Name);
                    continue;
                }
                bool isMainModule = ReferenceEquals(module, _module);
                bool useAutoDiAssemblies = settings.Behavior.HasFlag(Behaviors.IncludeDependentAutoDIAssemblies);
                bool matchesAssembly = settings.Assemblies.Any(a => a.Matches(module.Assembly));
                if (isMainModule || useAutoDiAssemblies || matchesAssembly)
                {
                    //Check if it references AutoDI. If it doesn't we will skip
                    //We also always process the main module since the weaver was directly added to it
                    if (!isMainModule && !matchesAssembly &&
                        module.AssemblyReferences.All(a => a.Name != autoDIFullName))
                    {
                        continue;
                    }
                    _logger.Debug($"Including types from '{module.Assembly.FullName}'", DebugLogLevel.Default);
                    //Either references AutoDI, or was a config assembly match, include the types.
                    foreach (TypeDefinition type in FilterTypes(module.GetAllTypes()))
                    {
                        allTypes.Add(type);
                    }
                }
            }
            return allTypes;
        }

        public IEnumerable<ModuleDefinition> GetAllModules()
        {
            var seen = new HashSet<string>();
            var queue = new Queue<ModuleDefinition>();
            queue.Enqueue(_module);

            while (queue.Count > 0)
            {
                ModuleDefinition module = queue.Dequeue();
                yield return module;

                foreach (AssemblyNameReference assemblyReference in module.AssemblyReferences)
                {
                    if (seen.Contains(assemblyReference.FullName)) continue;
                    AssemblyDefinition assembly;
                    try
                    {
                        assembly = _assemblyResolver.Resolve(assemblyReference);
                    }
                    catch (AssemblyResolutionException)
                    {
                        continue;
                    }
                    if (assembly?.MainModule == null)
                    {
                        continue;
                    }
                    seen.Add(assembly.FullName);
                    queue.Enqueue(assembly.MainModule);
                }
            }
        }
    }
}