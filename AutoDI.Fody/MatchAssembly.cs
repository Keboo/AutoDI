namespace AutoDI.Fody
{
    internal class MatchAssembly
    {
        private readonly Matcher _assemblyNameMatcher;

        public MatchAssembly(string assemblyName)
        {
            _assemblyNameMatcher = new Matcher(assemblyName);
        }

        public bool Matches(string assemblyName) => _assemblyNameMatcher.TryMatch(assemblyName, out _);

        public override string ToString()
        {
            return _assemblyNameMatcher.ToString();
        }
    }
}