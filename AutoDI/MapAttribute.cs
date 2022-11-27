namespace AutoDI;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class MapAttribute : Attribute
{
    public string? SourceTypePattern { get; set; }
    public string? TargetTypePattern { get; set; }
    public bool Force { get; set; }

    public Lifetime Lifetime { get; }

    public MapAttribute(Lifetime lifetime)
    {
        Lifetime = lifetime;
    }

    public MapAttribute(Type targetType, Lifetime lifetime)
        : this(lifetime)
    {
        TargetTypePattern = targetType.FullName;
    }

    public MapAttribute(string targetTypePattern, Lifetime lifetime)
        : this(lifetime)
    {
        TargetTypePattern = targetTypePattern;
    }

    public MapAttribute(string sourceTypePattern, string targetTypePattern)
    {
        SourceTypePattern = sourceTypePattern;
        TargetTypePattern = targetTypePattern;
    }

    public MapAttribute(Type sourceType, Type targetType)
    {
        SourceTypePattern = sourceType.FullName;
        TargetTypePattern = targetType.FullName;
    }

    public MapAttribute(string sourceTypePattern, string targetTypePattern, Lifetime lifetime)
    {
        SourceTypePattern = sourceTypePattern;
        TargetTypePattern = targetTypePattern;
        Lifetime = lifetime;
    }

    public MapAttribute(Type sourceType, Type targetType, Lifetime lifetime)
    {
        SourceTypePattern = sourceType.FullName;
        TargetTypePattern = targetType.FullName;
        Lifetime = lifetime;
    }
}