using System.Text.RegularExpressions;

namespace AutoDI.Fody
{
    internal class Map
    {
        private readonly Matcher _matcher;

        public bool Force { get; }

        public Map(string from, string to, bool force)
        {
            _matcher = new Matcher(from, to);
            Force = force;
        }

        public bool TryGetMap(string fromType, out string mappedType)
        {
            return _matcher.TryMatch(fromType, out mappedType);
        }

        public override string ToString() => _matcher.ToString();
    }
}