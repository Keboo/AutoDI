using AutoDI.AssemblyGenerator;
using System;
using System.Linq;

namespace AutoDI.Fody.Tests
{
    public static class MapMixins
    {
        public static bool IsMapped<TKey, TValue>(this IContainer container, Type containerType = null)
        {
            return container.GetMappings().Any(map => map.SourceType.Is<TKey>(containerType) &&
                                                      map.TargetType.Is<TValue>(containerType));
        }
    }
}