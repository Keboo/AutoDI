using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoDI
{
    public static class ServiceCollectionMixins
    {
        public static void AddAutoDIService<T>(this IServiceCollection serviceCollection, Func<IServiceProvider, object> factory, 
            Type[] serviceTypes, Lifetime lifetime)
        {
            Func<IServiceProvider, object> lifetimeFactory = WithLifetime(factory, lifetime);
            foreach (Type serviceType in serviceTypes)
            {
                serviceCollection.Add(new AutoDIServiceDescriptor(serviceType, typeof(T), lifetimeFactory, lifetime));
            }
        }

        private static Func<IServiceProvider, object> WithLifetime(Func<IServiceProvider, object> factory,
            Lifetime lifetime)
        {
            switch (lifetime)
            {
                case Lifetime.Singleton:
                case Lifetime.LazySingleton:
                {
                    var syncLock = new object();
                    object value = null;
                    return provider =>
                    {
                        if (value != null) return value;
                        lock (syncLock)
                        {
                            if (value != null) return value;
                            return value = factory(provider);
                        }
                    };
                }
                case Lifetime.Scoped:
                    return factory;
                case Lifetime.WeakTransient:
                {
                    var weakRef = new WeakReference<object>(null);
                    return provider =>
                    {
                        lock (weakRef)
                        {
                            if (!weakRef.TryGetTarget(out object value))
                            {
                                value = factory(provider);
                                weakRef.SetTarget(value);
                            }
                            return value;
                        }
                    };
                }
                case Lifetime.Transient:
                    return factory;
                default:
                    throw new InvalidOperationException($"Unknown lifetime '{lifetime}'");
            }
        }

        //TODO More user friendly extension methods
    }
}