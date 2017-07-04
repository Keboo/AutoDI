using System.Text.RegularExpressions;

namespace AutoDI.Container.Fody
{
    internal class MatchAssembly
    {
        private readonly Regex _assemblyNameRegex;

        public MatchAssembly(string assemblyName)
        {
            _assemblyNameRegex = new Regex(assemblyName);
        }

        public bool Matches(string assemblyName) => _assemblyNameRegex.IsMatch(assemblyName);
    }
}