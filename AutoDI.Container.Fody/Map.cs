using System.Text.RegularExpressions;

namespace AutoDI.Container.Fody
{
    internal class Map
    {
        private readonly string _to;
        private readonly Regex _fromRegex;

        public bool Force { get; }

        public Map(string from, string to, bool force)
        {
            _to = to;
            _fromRegex = new Regex(from);
            Force = force;
        }

        public bool TryGetMap(string fromType, out string mappedType)
        {
            Match fromMatch = _fromRegex.Match(fromType);
            if (fromMatch.Success)
            {
                mappedType = _fromRegex.Replace(fromType, _to);
                return true;
            }
            mappedType = null;
            return false;
        }
    }
}