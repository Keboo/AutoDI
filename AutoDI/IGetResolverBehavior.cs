namespace AutoDI
{
    public interface IGetResolverBehavior
    {
        IDependencyResolver Get(ResolverRequest request);
    }
}