using AutoDI.AssemblyGenerator;

namespace AutoDI.Build.Tests;

public static class MapMixins
{
    public static bool IsMapped<TKey, TValue>(this IContainer container, Type containerType = null)
    {
        return container.Any(map => map.SourceType.Is<TKey>(containerType) &&
                                                  map.TargetType.Is<TValue>(containerType));
    }
}