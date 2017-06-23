using System;

namespace AutoDI.AssemblyGenerator
{
    public static class TypeMixins
    {
        public static bool Is<TExpected>(this object @object)
        {
            return string.Equals(@object?.GetType().FullName, typeof(TExpected).FullName);
        }

        public static bool Is<TExpected>(this Type type)
        {
            return string.Equals(type?.FullName, typeof(TExpected).FullName);
        }
    }
}