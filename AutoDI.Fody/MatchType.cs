namespace AutoDI.Fody
{
    internal class MatchType
    {
        private readonly Matcher _matcher;
        public MatchType(string type, Lifetime lifetime)
        {
            _matcher = new Matcher(type);
            Lifetime = lifetime;
        }

        public Lifetime Lifetime { get; }

        public bool Matches(string type) => _matcher.TryMatch(type, out string _);

        public override string ToString()
        {
            return $"'{_matcher}' => {Lifetime}";
        }
    }
}