namespace AutoDI
{
    public static class DependencyResolver
    {
        private static IGetResolverBehavior _behavior;

        public static IDependencyResolver Get( ResolverRequest request )
        {
            return _behavior?.Get( request );
        }

        public static void Set( IDependencyResolver resolver )
        {
            Set( new StaticInstanceBehavior( resolver ) );
        }

        public static void Set( IGetResolverBehavior behavior )
        {
            _behavior = behavior;
        }
    }
}