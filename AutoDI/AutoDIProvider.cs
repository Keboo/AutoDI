using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoDI
{

    internal class AutoDIServiceProvider : IServiceProvider, IAutoDISerivceProvider
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

        public object GetService(Type serviceType, object[] parameters)
        {
            //TODO: use parameters
            return _containerMap.Get(serviceType);
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
                return descriptor.Lifetime.ToAutoDI();
            }

            //TODO: actually register the items in the container map
            foreach (ServiceDescriptor serviceDescriptor in services)
            {
                switch (GetLifetime(serviceDescriptor))
                {
                    //TODO....
                    case Lifetime.Singleton:
                        break;
                    case Lifetime.LazySingleton:
                        break;
                    case Lifetime.Scoped:
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

    //This class will be generated
    internal static class AutoDI_Gen
    {
        //This is the default resolver that will be used when nothing else specified
        private static IServiceProvider _globalServiceProvider;

        public static IServiceProvider Global
        {
            get
            {
                if (_globalServiceProvider == null)
                {
                    throw new InvalidOperationException("AutoDI has not been initialized");
                }
                return _globalServiceProvider;
            }
        }

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
            collection.AddSingleton<IServiceExample, ServiceExample>();
            collection.AddAutoDIService<ServiceExample>(sp => new ServiceExample(), new[] {typeof(IServiceExample)}, Lifetime.LazySingleton);
            collection.AddAutoDIService<ExampleManager>(
                sp => new ExampleManager(sp.GetService<IServiceExample>(), sp.GetService<IServiceExample>()), 
                new[] {typeof(IServiceExample), typeof(IServiceProvider)}, 
                Lifetime.LazySingleton);
            
        }
    }

    public interface IServiceExample
    { }

    public class ServiceExample : IServiceExample { }

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

    public class ExampleManager
    {
        public ExampleManager([Dependency]IServiceExample service = null, IServiceExample service2 = null)
        {
            if (service == null)
            {
                service = AutoDI_Gen.Global.GetService<IServiceExample>(new object[0]);
            }
        }
    }
}