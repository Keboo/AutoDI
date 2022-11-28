namespace AutoDI;

public abstract class BaseResolver : IAutoDISerivceProvider, IInitializeServiceProvider
{
    public virtual T? Resolve<T>(params object[] parameters)
    {
        return Resolve(typeof(T), parameters) is T result ? result : default;
    }

    public abstract object Resolve(Type desiredType, params object[] parameters);

    public virtual void Initialize()
    {
        //Base implementation does nothing
    }

    public object GetService(Type serviceType, object[] parameters)
    {
        throw new NotImplementedException();
    }
}