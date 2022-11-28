using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoDI.Build;

internal class TypeResolver
{
    private const string AutoDIAssemblyName = "AutoDI";

    private AssemblyRewiteTaskContext Context { get; }

    public TypeResolver(AssemblyRewiteTaskContext context)
    {
        Context = context;
    }

    public ICollection<TypeDefinition> GetAllTypes(Settings settings)
    {
        var allTypes = new HashSet<TypeDefinition>(TypeComparer.FullName);
        IEnumerable<TypeDefinition> FilterTypes(IEnumerable<TypeDefinition> types) =>
            types.Where(t => !t.IsCompilerGenerated() && !allTypes.Remove(t));

        foreach (ModuleDefinition module in GetAllModules(settings))
        {
            Context.Debug($"Processing module for {module.FileName}", DebugLogLevel.Verbose);
            
            Context.Debug($"Including types from '{module.Assembly.FullName}' ({GetIncludeReason()})", DebugLogLevel.Default);
            //Either references AutoDI, or was a config assembly match, include the types.
            foreach (TypeDefinition type in FilterTypes(module.GetAllTypes()))
            {
                allTypes.Add(type);
            }

            string GetIncludeReason()
            {
                bool isMainModule = ReferenceEquals(module, Context.ModuleDefinition);

                if (isMainModule) return "Main Module";

                bool matchesAssembly = settings.Assemblies.Any(a => a.Matches(module.Assembly));
                if (matchesAssembly)
                {
                    var match = settings.Assemblies.FirstOrDefault(a => a.Matches(module.Assembly));
                    return $"Matches included assembly: {match}";
                }

                bool useAutoDiAssemblies = settings.Behavior.HasFlag(Behaviors.IncludeDependentAutoDIAssemblies);
                return useAutoDiAssemblies ? $"References {AutoDIAssemblyName}" : "Unknown";
            }
        }
        return allTypes;
    }

    private IEnumerable<ModuleDefinition> GetAllModules(Settings settings)
    {
        var seen = new HashSet<string>();
        var queue = new Queue<ModuleDefinition>();
        queue.Enqueue(Context.ModuleDefinition);

        while (queue.Count > 0)
        {
            ModuleDefinition module = queue.Dequeue();
            yield return module;

            foreach (AssemblyNameReference assemblyReference in module.AssemblyReferences)
            {
                if (!seen.Add(assemblyReference.FullName)) continue;

                bool matchesAssembly = settings.Assemblies.Any(a => a.Matches(assemblyReference));
                bool useAutoDiAssemblies = settings.Behavior.HasFlag(Behaviors.IncludeDependentAutoDIAssemblies);

                if (!matchesAssembly && !useAutoDiAssemblies) continue;

                AssemblyDefinition assembly;
                try
                {
                    assembly = Context.AssemblyResolver.Resolve(assemblyReference);

                    if (assembly?.MainModule is null)
                    {
                        continue;
                    }

                    if (!matchesAssembly && assembly.MainModule.AssemblyReferences.All(x => x.Name != AutoDIAssemblyName))
                    {
                        continue;
                    }
                }
                catch (AssemblyResolutionException)
                {
                    continue;
                }
                queue.Enqueue(assembly.MainModule);
            }
        }
    }
}