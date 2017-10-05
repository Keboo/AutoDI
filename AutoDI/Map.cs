using System;

namespace AutoDI
{
    public class Map
    {
        public Type SourceType { get; }
        public Type TargetType { get; }
        public Lifetime LifetimeMode { get; }

        internal Map(Type sourceType, Type targetType, Lifetime lifetimeMode)
        {
            SourceType = sourceType;
            TargetType = targetType;
            LifetimeMode = lifetimeMode;
        }

        public override string ToString()
        {
            return $"{SourceType.FullName} -> {TargetType.FullName} ({LifetimeMode})";
        }
    }
}