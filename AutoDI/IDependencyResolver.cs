namespace AutoDI
{
    public interface IDependencyResolver
    {
        T Resolve<T>(params object[] parameters);
    }
}