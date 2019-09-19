using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Tests
{
    [TestClass]
    public class ApplicationBuilderTests
    {
        [TestMethod]
        public void CanReplaceContainer()
        {
            IContainer container = null;
            var applicationBuilder = new ApplicationBuilder();
            applicationBuilder.ConfigureServices(sc =>
            {
                sc.AddAutoDISingleton<IServiceProviderFactory<IContainer>, ContainerServiceProviderFactory>();
            });
            applicationBuilder.ConfigureContainer<IContainer>(c => { container = c; });

            IServiceProvider sp = applicationBuilder.Build();
            Assert.IsNotNull(sp);
            Assert.IsNotNull(container);
        }

        private class ContainerServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }

        private class Container : IContainer { }

        private interface IContainer
        { }

        private interface IRegistrator
        { }

        private class ContainerServiceProviderFactory : IServiceProviderFactory<IContainer>
        {
            //This is simulating DryIocServiceProviderFactory
            public ContainerServiceProviderFactory(IContainer container = null,
                Func<IRegistrator, ServiceDescriptor, bool> registerDescriptor = null)
            {
                
            }

            public IContainer CreateBuilder(IServiceCollection services)
            {
                return new Container();
            }

            public IServiceProvider CreateServiceProvider(IContainer containerBuilder)
            {
                return new ContainerServiceProvider();
            }
        }
    }
}