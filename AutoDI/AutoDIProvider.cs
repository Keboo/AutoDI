using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoDI
{
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
            if (_globalServiceProvider != null)
                throw new InvalidOperationException(
                    "AutoDI has already been initialized. Call Dispose before trying to initialize a second time.");
            IApplicationBuilder builder = new ApplicationBuilder();
            builder.ConfigureServices(Gen_Configured);
            configure?.Invoke(builder);

            _globalServiceProvider = builder.Build();
        }

        public static void Dispose()
        {
            if (_globalServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _globalServiceProvider = null;
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
            applicationBuilder.ConfigureContinaer<IContainer>(map =>
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