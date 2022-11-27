using Mono.Cecil;

namespace AutoDI.Build;

internal class MatchAssembly
{
    private readonly Matcher<string> _assemblyNameMatcher;

    public MatchAssembly(string assemblyName)
    {
        _assemblyNameMatcher = new Matcher<string>(x => x, assemblyName);
    }

    public bool Matches(AssemblyDefinition assemblyName) => _assemblyNameMatcher.TryMatch(assemblyName.FullName, out _);
    public bool Matches(AssemblyNameReference assemblyName) => _assemblyNameMatcher.TryMatch(assemblyName.FullName, out _);

    public override string ToString()
    {
        return _assemblyNameMatcher.ToString();
    }
}