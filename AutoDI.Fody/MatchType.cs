using System.Text.RegularExpressions;

namespace AutoDI.Fody
{
    internal class MatchType
    {
        private readonly Regex _typeRegex;
        public MatchType(string type, Lifetime lifetime)
        {
            _typeRegex = new Regex(type);
            Lifetime = lifetime;
        }

        public Lifetime Lifetime { get; }

        public bool Matches(string type) => _typeRegex.IsMatch(type);

        public override string ToString()
        {
            return $"'{_typeRegex}' => {Lifetime}";
        }
    }
}