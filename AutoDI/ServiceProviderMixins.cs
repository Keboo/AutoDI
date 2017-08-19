using System;

namespace AutoDI
{
    public static class ServiceProviderMixins
    {
        public static T GetService<T>(this IServiceProvider provider, object[] autoDiParameters)
        {
            return (T)provider.GetService(typeof(T), autoDiParameters);
            
        }

        public static object GetService(this IServiceProvider provider, Type serviceType, object[] autoDiParameters)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            if (provider is IAutoDISerivceProvider autoDiProvider)
            {
                return autoDiProvider.GetService(serviceType, autoDiParameters);
            }
            return provider.GetService(serviceType);
        }
    }
}