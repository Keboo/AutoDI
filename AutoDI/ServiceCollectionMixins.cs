using AutoDI;
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class AutoDIServiceCollectionMixins
    {
        public static IServiceCollection AddAutoDIService(this IServiceCollection services, Type serviceType,
            Type implementationType, Lifetime lifetime)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.Add(new AutoDIServiceDescriptor(serviceType, implementationType, lifetime));
            return services;
        }

        public static IServiceCollection AddAutoDIService(this IServiceCollection services, Type serviceType,
            Type implementationType, Func<IServiceProvider, object> implementationFactory, Lifetime lifetime)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.Add(new AutoDIServiceDescriptor(serviceType, implementationType, implementationFactory, lifetime));
            return services;
        }

        public static IServiceCollection AddAutoDITransient(this IServiceCollection services, Type serviceType,
            Type implementationType)
        {
            return services.AddAutoDIService(serviceType, implementationType, Lifetime.Transient);
        }

        public static IServiceCollection AddAutoDITransient(this IServiceCollection services, Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            return services.AddAutoDIService(serviceType, serviceType, implementationFactory, Lifetime.Transient);
        }

        public static IServiceCollection AddAutoDITransient<TService, TImplementation>(this IServiceCollection services)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), Lifetime.Transient);
        }

        public static IServiceCollection AddAutoDITransient(this IServiceCollection services, Type serviceType)
        {
            return services.AddAutoDIService(serviceType, serviceType, Lifetime.Transient);
        }

        public static IServiceCollection AddAutoDITransient<TService>(this IServiceCollection services) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), Lifetime.Transient);
        }

        public static IServiceCollection AddAutoDITransient<TService>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), implementationFactory,
                Lifetime.Transient);
        }

        public static IServiceCollection AddAutoDITransient<TService, TImplementation>(this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), implementationFactory,
                Lifetime.Transient);
        }

        public static IServiceCollection AddAutoDIWeakSingleton(this IServiceCollection services, Type serviceType,
            Type implementationType)
        {
            return services.AddAutoDIService(serviceType, implementationType, Lifetime.WeakSingleton);
        }

        public static IServiceCollection AddAutoDIWeakSingleton(this IServiceCollection services, Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            return services.AddAutoDIService(serviceType, serviceType, implementationFactory, Lifetime.WeakSingleton);
        }

        public static IServiceCollection AddAutoDIWeakSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), Lifetime.WeakSingleton);
        }

        public static IServiceCollection AddAutoDIWeakSingleton(this IServiceCollection services, Type serviceType)
        {
            return services.AddAutoDIService(serviceType, serviceType, Lifetime.WeakSingleton);
        }

        public static IServiceCollection AddAutoDIWeakSingleton<TService>(this IServiceCollection services) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), Lifetime.WeakSingleton);
        }

        public static IServiceCollection AddAutoDIWeakSingleton<TService>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), implementationFactory,
                Lifetime.WeakSingleton);
        }

        public static IServiceCollection AddAutoDIWeakSingleton<TService, TImplementation>(this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), implementationFactory,
                Lifetime.WeakSingleton);
        }

        public static IServiceCollection AddAutoDIScoped(this IServiceCollection services, Type serviceType,
            Type implementationType)
        {
            return services.AddAutoDIService(serviceType, implementationType, Lifetime.Scoped);
        }

        public static IServiceCollection AddAutoDIScoped(this IServiceCollection services, Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            return services.AddAutoDIService(serviceType, serviceType, implementationFactory, Lifetime.Scoped);
        }

        public static IServiceCollection AddAutoDIScoped<TService, TImplementation>(this IServiceCollection services)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), Lifetime.Scoped);
        }

        public static IServiceCollection AddAutoDIScoped(this IServiceCollection services, Type serviceType)
        {
            return services.AddAutoDIService(serviceType, serviceType, Lifetime.Scoped);
        }

        public static IServiceCollection AddAutoDIScoped<TService>(this IServiceCollection services) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), Lifetime.Scoped);
        }

        public static IServiceCollection AddAutoDIScoped<TService>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), implementationFactory,
                Lifetime.Scoped);
        }

        public static IServiceCollection AddAutoDIScoped<TService, TImplementation>(this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), implementationFactory,
                Lifetime.Scoped);
        }

        public static IServiceCollection AddAutoDILazySingleton(this IServiceCollection services, Type serviceType,
            Type implementationType)
        {
            return services.AddAutoDIService(serviceType, implementationType, Lifetime.LazySingleton);
        }

        public static IServiceCollection AddAutoDILazySingleton(this IServiceCollection services, Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            return services.AddAutoDIService(serviceType, serviceType, implementationFactory, Lifetime.LazySingleton);
        }

        public static IServiceCollection AddAutoDILazySingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), Lifetime.LazySingleton);
        }

        public static IServiceCollection AddAutoDILazySingleton(this IServiceCollection services, Type serviceType)
        {
            return services.AddAutoDIService(serviceType, serviceType, Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDILazySingleton<TService>(this IServiceCollection services) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), Lifetime.LazySingleton);
        }

        public static IServiceCollection AddAutoDILazySingleton<TService>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), implementationFactory,
                Lifetime.LazySingleton);
        }

        public static IServiceCollection AddAutoDILazySingleton<TService, TImplementation>(this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), implementationFactory,
                Lifetime.LazySingleton);
        }

        public static IServiceCollection AddAutoDISingleton(this IServiceCollection services, Type serviceType,
            Type implementationType)
        {
            return services.AddAutoDIService(serviceType, implementationType, Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton(this IServiceCollection services, Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            return services.AddAutoDIService(serviceType, serviceType, implementationFactory, Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton(this IServiceCollection services, Type serviceType)
        {
            return services.AddAutoDIService(serviceType, serviceType, Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton<TService>(this IServiceCollection services) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton<TService>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), typeof(TService), implementationFactory,
                Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton<TService, TImplementation>(this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class where TImplementation : class, TService
        {
            return services.AddAutoDIService(typeof(TService), typeof(TImplementation), implementationFactory,
                Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton(this IServiceCollection services, Type serviceType,
            object implementationInstance)
        {
            return services.AddAutoDIService(serviceType, implementationInstance?.GetType(), provider => implementationInstance, Lifetime.Singleton);
        }

        public static IServiceCollection AddAutoDISingleton<TService>(this IServiceCollection services,
            TService implementationInstance) where TService : class
        {
            return services.AddAutoDIService(typeof(TService), implementationInstance?.GetType(), provider => implementationInstance,
                Lifetime.Singleton);
        }
    }
}