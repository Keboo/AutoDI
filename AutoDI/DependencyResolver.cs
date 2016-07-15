namespace AutoDI
{
    public static class DependencyResolver
    {
        private static IDependencyResolver _resolver;

        public static IDependencyResolver Get()
        {
            return _resolver;
        }

        public static void Set(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }
    }
}