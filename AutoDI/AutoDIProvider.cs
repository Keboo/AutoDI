using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace AutoDI
{
    public class AutoDIServiceProvider : IServiceProvider, ISupportRequiredService
    {
        public AutoDIServiceProvider(ContainerMap map)
        {
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public object GetRequiredService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }

    public class AutoDIServiceScope : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            throw new NotImplementedException();
        }
    }

    public class AutoDIServiceProviderFactory : IServiceProviderFactory<ContainerMap>
    {
        public ContainerMap CreateBuilder(IServiceCollection services)
        {
            var map = new ContainerMap();

            Lifetime GetLifetime(ServiceDescriptor descriptor)
            {
                if (descriptor is AutoDIServiceDescriptor autoDiDescriptor)
                {
                    return autoDiDescriptor.Lifetime;
                }
                //TODO: Translate
                return Lifetime.Transient;
            }

            foreach (ServiceDescriptor serviceDescriptor in services)
            {
                switch (GetLifetime(serviceDescriptor))
                {
                    case Lifetime.Singleton:
                        map.AddSingleton();
                        break;
                    case Lifetime.LazySingleton:

                        break;
                    case Lifetime.Transient:

                        break;
                }
            }

            return map;
        }

        public IServiceProvider CreateServiceProvider(ContainerMap containerBuilder)
        {
            return new AutoDIServiceProvider(containerBuilder);
        }
    }

    public class AutoDIServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            throw new NotImplementedException();
        }

        private class AutoDIServiceScope : IServiceScope
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IServiceProvider ServiceProvider { get; }
        }
    }

    public class AutoDIServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {

    }

    public class AutoDIServiceDescriptor : ServiceDescriptor
    {
        public Lifetime Lifetime { get; }

        public AutoDIServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime) : base(serviceType, implementationType, lifetime)
        {
        }

        public AutoDIServiceDescriptor(Type serviceType, object instance) : base(serviceType, instance)
        {
        }

        public AutoDIServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime) : base(serviceType, factory, lifetime)
        {
        }
    }

    public static class AutoDI
    {
        private static AutoDIServiceProvider _serviceProvider;

        public static void Init(Action<IServiceCollection> configureMethod)
        {
            var collection = new AutoDIServiceCollection();
            configureMethod(collection);

            _serviceProvider = new AutoDIServiceProvider(collection);
        }

        private static void Gen_Configured(AutoDIServiceCollection collection)
        {
            //TODO: All of the adds....
        }
    }

    public class P
    {
        public static void Main()
        {



        }

        public static void Configure(IServiceCollection serviceCollection)
        {

        }
    }
}