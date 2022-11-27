namespace AutoDI;

public static class ServiceProviderMixins
{
    public static T GetService<T>(this IServiceProvider provider, params object[] autoDiParameters)
    {
        return (T)provider.GetService(typeof(T), autoDiParameters);
    }

    public static object GetService(this IServiceProvider provider, Type serviceType, params object[] autoDiParameters)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }
        else
        {
            if (provider is IAutoDISerivceProvider autoDiProvider)
            {
                return autoDiProvider.GetService(serviceType, autoDiParameters);
            }
            else
            {
                return provider.GetService(serviceType);
            }
        }
    }
}