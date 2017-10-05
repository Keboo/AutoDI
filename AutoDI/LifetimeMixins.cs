using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    public static class LifetimeMixins
    {
        public static Lifetime ToAutoDI(this ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    return Lifetime.LazySingleton;
                case ServiceLifetime.Scoped:
                    return Lifetime.Scoped;
                case ServiceLifetime.Transient:
                    return Lifetime.Transient;
                default:
                    throw new InvalidOperationException($"Unknown {nameof(ServiceLifetime)} '{lifetime}'");
            }
        }

        public static ServiceLifetime FromAutoDI(this Lifetime lifetime)
        {
            switch (lifetime)
            {
                case Lifetime.Singleton:
                case Lifetime.LazySingleton:
                    return ServiceLifetime.Singleton;
                case Lifetime.Scoped:
                    return ServiceLifetime.Scoped;
                case Lifetime.WeakTransient:
                case Lifetime.Transient:
                    return ServiceLifetime.Transient;
                default:
                    throw new InvalidOperationException($"Unknown {nameof(Lifetime)} '{lifetime}'");
            }
        }
    }
}