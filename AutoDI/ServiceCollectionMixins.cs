using System;
using AutoDI;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    public static class ServiceCollectionMixins
    {
        public static void AddAutoDIService<T>(this IServiceCollection serviceCollection, Func<IServiceProvider, T> factory, 
            Type[] serviceTypes, Lifetime lifetime)
        {
            foreach (Type serviceType in serviceTypes)
            {
                serviceCollection.Add(new AutoDIServiceDescriptor(serviceType, sp => factory(sp), lifetime.FromAutoDI()));
            }
        }

        //TODO More user friendly extension methods
    }
}