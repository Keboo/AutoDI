namespace AutoDI;

internal class AutoDIServiceProvider : IServiceProvider, IAutoDISerivceProvider, IInitializeServiceProvider
{
    internal IContainer ContainerMap { get; }

    public AutoDIServiceProvider(IContainer containerMap)
    {
        ContainerMap = containerMap ?? throw new ArgumentNullException(nameof(containerMap));
    }

    object? IServiceProvider.GetService(Type serviceType)
    {
        return ContainerMap.Get(serviceType, this);
    }

    object? IAutoDISerivceProvider.GetService(Type serviceType, object[] parameters)
    {
        //TODO: use parameters
        return ContainerMap.Get(serviceType, this);
    }

    void IInitializeServiceProvider.Initialize()
    {
        (ContainerMap as ContainerMap)?.CreateSingletons(this);
    }
}