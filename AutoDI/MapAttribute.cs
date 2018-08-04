using System;

namespace AutoDI
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class MapAttribute : Attribute
    {
        public string TargetTypePattern { get; set; }

        public Lifetime Lifetime { get; }

        public MapAttribute(Lifetime lifetime)
        {
            Lifetime = lifetime;
        }

        public MapAttribute(Lifetime lifetime, Type targetType)
            : this(lifetime)
        {
            TargetTypePattern = targetType.FullName;
        }

        public MapAttribute(Lifetime lifetime, string targetTypePattern)
            : this(lifetime)
        {
            TargetTypePattern = targetTypePattern;
        }
    }
}