using System.Text.RegularExpressions;

namespace AutoDI.Fody
{
    internal class MatchAssembly
    {
        private readonly Regex _assemblyNameRegex;

        public MatchAssembly(string assemblyName)
        {
            _assemblyNameRegex = new Regex(assemblyName);
        }

        public bool Matches(string assemblyName) => _assemblyNameRegex.IsMatch(assemblyName);

        public override string ToString()
        {
            return _assemblyNameRegex.ToString();
        }
    }
}