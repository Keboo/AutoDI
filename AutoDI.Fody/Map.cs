using Mono.Cecil;

namespace AutoDI.Fody
{
    internal class Map
    {
        private readonly Matcher<TypeDefinition> _matcher;

        public bool Force { get; }

        public Map(string from, string to, bool force)
        {
            _matcher = new Matcher<TypeDefinition>(type => type.FullName, from, to);
            _matcher.AddVariable("ns", type => type.Namespace);
            _matcher.AddVariable("fn", type => type.FullName);
            _matcher.AddVariable("name", type => type.Name);
            Force = force;
        }

        public bool TryGetMap(TypeDefinition fromType, out string mappedType)
        {
            return _matcher.TryMatch(fromType, out mappedType);
        }

        public override string ToString() => _matcher.ToString();
    }
}