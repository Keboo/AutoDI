namespace AutoDI;

public interface IContainer : IEnumerable<Map>
{
    event EventHandler<TypeKeyNotFoundEventArgs> TypeKeyNotFound;

    T Get<T>(IServiceProvider? provider);
    object Get(Type serviceType, IServiceProvider provider);

    bool Remove<T>();
    bool Remove(Type serviceType);

    IContainer CreatedNestedContainer();
}