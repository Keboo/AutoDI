using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoDI
{

    internal class AutoDIServiceProvider : IServiceProvider, ISupportRequiredService
    {
        private readonly ContainerMap _containerMap;

        public AutoDIServiceProvider(ContainerMap containerMap)
        {
            _containerMap = containerMap ?? throw new ArgumentNullException(nameof(containerMap));
        }

        public object GetService(Type serviceType)
        {
            return _containerMap.Get(serviceType);
        }

        public object GetRequiredService(Type serviceType)
        {
            object rv = _containerMap.Get(serviceType);
            //TODO: Better checking and exception
            if (rv == null) throw new Exception($"Required service '{serviceType?.FullName}' not found");
            return rv;
        }
    }

    internal class AutoDIServiceProviderFactory : IServiceProviderFactory<ContainerMap>
    {
        public ContainerMap CreateBuilder(IServiceCollection services)
        {
            var map = new ContainerMap();

            Lifetime GetLifetime(ServiceDescriptor descriptor)
            {
                if (descriptor is AutoDIServiceDescriptor autoDiDescriptor)
                {
                    return autoDiDescriptor.AutoDILifetime;
                }
                switch (descriptor.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        return Lifetime.Singleton;
                    case ServiceLifetime.Scoped:
                        return Lifetime.Scoped;
                    default:
                        return Lifetime.Transient;
                }
            }

            //TODO: actually register the items in the container map
            foreach (ServiceDescriptor serviceDescriptor in services)
            {
                switch (GetLifetime(serviceDescriptor))
                {
                    case Lifetime.Singleton:
                        break;
                    case Lifetime.LazySingleton:
                        break;
                    case Lifetime.WeakTransient:
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

    internal class AutoDIServiceScopeFactory : IServiceScopeFactory
    {
        private readonly ContainerMap _map;

        public AutoDIServiceScopeFactory(ContainerMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public IServiceScope CreateScope()
        {
            //TODO: anything needed for this? SM uses this call to created the nested container here
            //Probably want to clone the container map here.
            return new AutoDIServiceScope(_map);
        }

        private class AutoDIServiceScope : IServiceScope
        {
            private readonly ContainerMap _map;


            public AutoDIServiceScope(ContainerMap map)
            {
                _map = map;
                //TODO: Is this correct?
                ServiceProvider = map.Get<IServiceProvider>();
            }

            public void Dispose()
            {
                //TODO: Any clenaup needed?
            }

            public IServiceProvider ServiceProvider { get; }
        }
    }

    internal class AutoDIServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {

    }

    internal class AutoDIServiceDescriptor : ServiceDescriptor
    {
        //TODO: Ctors that allow setting there.... we probably will only need the factory ctor
        public Lifetime AutoDILifetime { get; }

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

    public interface IApplicationBuilder
    {
        IApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        IApplicationBuilder ConfigureContinaer<TContainerType>(Action<TContainerType> configureContianer);

        IServiceProvider Build();
    }

    internal class ApplicationBuilder : IApplicationBuilder
    {
        private readonly List<Action<IServiceCollection>> _configureServicesDelegates = new List<Action<IServiceCollection>>();
        //TODO: this really should be strongly typed....
        private readonly List<Delegate> _configureContainerDelegates = new List<Delegate>();

        private Type _specifiedContainerType;

        public IApplicationBuilder ConfigureContinaer<TContainerType>(Action<TContainerType> configureContianer)
        {
            if (configureContianer == null) throw new ArgumentNullException(nameof(configureContianer));
            if (_specifiedContainerType != null) throw new InvalidOperationException($"A container type of '{_specifiedContainerType.FullName}' was already specified");
            _specifiedContainerType = typeof(TContainerType);
            _configureContainerDelegates.Add(configureContianer);
            return this;
        }

        public IApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            if (configureServices == null) throw new ArgumentNullException(nameof(configureServices));
            _configureServicesDelegates.Add(configureServices);
            return this;
        }

        public IServiceProvider Build()
        {
            IServiceCollection collection = BuildCommonServices();
            IServiceProvider applicationProvider = BuildApplicationServiceProvider(collection);
            IServiceProvider rootProvider = GetProvider(applicationProvider, collection);
            return rootProvider;
        }

        private Type GetContainerType(IServiceCollection serviceCollection)
        {
            if (_specifiedContainerType != null)
            {
                return _specifiedContainerType;
            }
            var containerTypes = from ServiceDescriptor serviceDescriptor in serviceCollection
                                 let typeInfo = serviceDescriptor.ServiceType.GetTypeInfo()
                                 where typeInfo.IsGenericTypeDefinition
                                 let genericType = typeInfo.GetGenericTypeDefinition()
                                 where genericType == typeof(IServiceProviderFactory<>)
                                 let containerType = serviceDescriptor.ServiceType.GenericTypeArguments[0]
                                 orderby containerType == typeof(ContainerMap) ? 1 : 0
                                 select containerType;
            return containerTypes.FirstOrDefault();
        }

        private IServiceProvider GetProvider(IServiceProvider applicationProvider,
            IServiceCollection serviceCollection)
        {
            return (IServiceProvider)GetType().GetTypeInfo().DeclaredMethods
                .Single(m => m.IsGenericMethodDefinition && m.Name == nameof(GetProvider))
                .MakeGenericMethod(GetContainerType(serviceCollection))
                .Invoke(this, new object[] { applicationProvider, serviceCollection });
        }

        private IServiceProvider GetProvider<TContainerType>(IServiceProvider applicationProvider, IServiceCollection serviceCollection)
        {
            IServiceProviderFactory<TContainerType> providerFactory = applicationProvider.GetService<IServiceProviderFactory<TContainerType>>();
            TContainerType container = providerFactory.CreateBuilder(serviceCollection);

            foreach (Action<TContainerType> configureMethods in _configureContainerDelegates.OfType<Action<TContainerType>>())
            {
                configureMethods(container);
            }

            return providerFactory.CreateServiceProvider(container);
        }

        private IServiceCollection BuildCommonServices()
        {
            var collection = new AutoDIServiceCollection();
            AutoDIStuff(collection);

            foreach (var @delegate in _configureServicesDelegates)
            {
                @delegate(collection);
            }

            return collection;
        }

        private static IServiceProvider BuildApplicationServiceProvider(IServiceCollection collection)
        {
            var factory = new AutoDIServiceProviderFactory();
            return factory.CreateServiceProvider(factory.CreateBuilder(collection));
        }

        private static void AutoDIStuff(AutoDIServiceCollection collection)
        {
            collection.AddSingleton<IServiceProviderFactory<ContainerMap>>(sp => new AutoDIServiceProviderFactory());
            //TODO: how to register the container map?
            collection.AddSingleton<IServiceScopeFactory>(sp => new AutoDIServiceScopeFactory(sp.GetService<ContainerMap>()));
            collection.AddScoped<IServiceProvider>(sp => new AutoDIServiceProvider(sp.GetService<ContainerMap>()));
        }
    }

    //This class will be generated
    internal static class AutoDI_Gen
    {
        //This is the default resolver that will be used when nothing else specified
        private static IServiceProvider _globalServiceProvider;

        public static void Init(Action<IApplicationBuilder> configure)
        {
            IApplicationBuilder builder = new ApplicationBuilder();
            builder.ConfigureServices(Gen_Configured);
            configure?.Invoke(builder);

            _globalServiceProvider = builder.Build();
        }

        private static void Gen_Configured(IServiceCollection collection)
        {
            //AuotDI generates all of the registrations here
        }
    }

    public class ExampleProgram
    {
        public static void Main()
        {
            //This line gets injected
            AutoDI_Gen.Init(ConfigureApplication);

            //The rest of your code here
        }

        //Optional, you write this method
        [SetupMethod]
        public static void ConfigureApplication(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.ConfigureServices(services => { });
            applicationBuilder.ConfigureContinaer<ContainerMap>(map =>
            {

            });
        }
    }
}