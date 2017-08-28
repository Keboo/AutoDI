using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoDI
{
    public static class ServiceCollectionMixins
    {
        public static void AddAutoDIService<T>(this IServiceCollection serviceCollection, Func<IServiceProvider, object> factory,
            Type[] serviceTypes, Lifetime lifetime)
        {
            //Func<IServiceProvider, object> lifetimeFactory = WithLifetime(factory, lifetime);
            foreach (Type serviceType in serviceTypes)
            {
                serviceCollection.Add(new AutoDIServiceDescriptor(serviceType, typeof(T), factory, lifetime));
            }
        }
        //TODO More user friendly extension methods
    }
}