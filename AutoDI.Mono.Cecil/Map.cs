using Mono.Cecil;

namespace AutoDI.Mono.Cecil
{
    internal class Map
    {
        private readonly Matcher<TypeDefinition> _matcher;

        public bool Force { get; }

        public Lifetime? Lifetime { get; }

        public Map(string from, string to, bool force, Lifetime? lifetime)
        {
            _matcher = new Matcher<TypeDefinition>(type => type.FullName, from, to);
            Force = force;
            Lifetime = lifetime;
        }

        public bool TryGetMap(TypeDefinition fromType, out string mappedType)
        {
            return _matcher.TryMatch(fromType, out mappedType);
        }

        public override string ToString() => _matcher.ToString();
    }
}