using Microsoft.Extensions.DependencyInjection;

namespace AutoDI;

public static class LifetimeMixins
{
    public static Lifetime ToAutoDI(this ServiceLifetime lifetime)
    {
        return lifetime switch
        {
            ServiceLifetime.Singleton => Lifetime.LazySingleton,
            ServiceLifetime.Scoped => Lifetime.Scoped,
            ServiceLifetime.Transient => Lifetime.Transient,
            _ => throw new InvalidOperationException($"Unknown {nameof(ServiceLifetime)} '{lifetime}'"),
        };
    }

    public static ServiceLifetime FromAutoDI(this Lifetime lifetime)
    {
        return lifetime switch
        {
            Lifetime.Singleton or Lifetime.LazySingleton or Lifetime.WeakSingleton => ServiceLifetime.Singleton,
            Lifetime.Scoped => ServiceLifetime.Scoped,
            Lifetime.Transient => ServiceLifetime.Transient,
            _ => throw new InvalidOperationException($"Unknown {nameof(Lifetime)} '{lifetime}'"),
        };
    }
}