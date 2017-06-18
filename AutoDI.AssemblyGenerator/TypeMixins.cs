namespace AutoDI.AssemblyGenerator
{
    public static class TypeMixins
    {
        public static bool Is<TExpected>(this object @object)
        {
            return string.Equals(@object?.GetType().FullName, typeof(TExpected).FullName);
        }
    }
}