namespace AutoDI
{
    public class StaticInstanceBehavior : IGetResolverBehavior
    {
        private readonly IDependencyResolver _resolver;

        public StaticInstanceBehavior(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public IDependencyResolver Get(ResolverRequest request)
        {
            return _resolver;
        }
    }
}