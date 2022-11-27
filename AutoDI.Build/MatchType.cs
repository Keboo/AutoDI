namespace AutoDI.Build;

internal class MatchType
{
    private readonly Matcher<string> _matcher;
    public MatchType(string type, Lifetime lifetime)
    {
        _matcher = new Matcher<string>(x => x, type);
        Lifetime = lifetime;
    }

    public Lifetime Lifetime { get; }

    public bool Matches(string type) => _matcher.TryMatch(type, out string _);

    public override string ToString()
    {
        return $"'{_matcher}' => {Lifetime}";
    }
}