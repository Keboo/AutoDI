using System;

namespace AutoDI.AssemblyGenerator
{
    public static class TypeMixins
    {
        public static bool Is<TExpected>(this object @object, Type containerType = null)
        {
            return Is(@object?.GetType(), typeof(TExpected), containerType);
        }

        public static bool Is<TExpected>(this Type type, Type containerType = null)
        {
            return Is(type, typeof(TExpected), containerType);
        }

        private static bool Is(Type typeA, Type typeB, Type containerType)
        {
            return string.Equals(GetTypeName(typeA, containerType), GetTypeName(typeB, containerType),
                StringComparison.Ordinal);
        }

        public static string GetTypeName(Type type, Type containerType)
        {
            if (type == null) return null;
            string rv = type.FullName;

            if (containerType?.Namespace != null && rv.StartsWith(containerType.Namespace))
            {
                rv = rv.Substring(containerType.Namespace.Length).TrimStart('.');
            }
            return rv;
        }
    }
}