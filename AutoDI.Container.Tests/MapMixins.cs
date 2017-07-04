using System;
using System.Linq;
using AutoDI.AssemblyGenerator;

namespace AutoDI.Container.Tests
{
    public static class MapMixins
    {
        public static bool IsMapped<TKey, TValue>(this ContainerMap container, Type containerType = null)
        {
            return container.GetMappings().Any(map => map.SourceType.Is<TKey>(containerType) &&
                                                      map.TargetType.Is<TValue>(containerType));
        }
    }
}