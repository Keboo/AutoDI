using System.Text.RegularExpressions;

namespace AutoDI.Container.Fody
{
    internal class MatchType
    {
        private readonly Regex _typeRegex;
        public MatchType(string type, Create create)
        {
            _typeRegex = new Regex(type);
            Create = create;
        }

        public Create Create { get; }

        public bool Matches(string type) => _typeRegex.IsMatch(type);
    }
}