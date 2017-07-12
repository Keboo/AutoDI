using System;
using Mono.Cecil;

namespace AutoDI.Container.Fody
{
    internal static class TypeReferenceMixins
    {
        internal static bool IsType<T>(this TypeReference reference)
        {
            return IsType(reference, typeof(T));
        }

        internal static bool IsType(this TypeReference reference, Type type)
        {
            return string.Equals(reference.FullName, type.FullName, StringComparison.Ordinal);
        }
    }
}