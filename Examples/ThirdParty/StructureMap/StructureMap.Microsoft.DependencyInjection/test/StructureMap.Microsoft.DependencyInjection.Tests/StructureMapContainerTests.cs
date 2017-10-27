using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection.Specification;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace StructureMap.Microsoft.DependencyInjection.Tests
{
    public class StructureMapContainerTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection services)
        {
            var container = new Container();

            container.Populate(services);

            return container.GetInstance<IServiceProvider>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PopulatingTheContainerMoreThanOnceThrows(bool checkDuplicateCalls)
        {
            var services = new ServiceCollection();

            services.AddTransient<IFakeService, FakeService>();

            var container = new Container();

            container.Configure(config => config.Populate(services));

            if (checkDuplicateCalls)
            {
                Assert.Throws<InvalidOperationException>(() => container.Populate(services, checkDuplicateCalls));
            }
        }

        [Fact] // See GitHub issue #32
        public void CanResolveIEnumerableWithDefaultConstructor()
        {
            var services = new ServiceCollection
            {
                ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>(),
                ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)),
                ServiceDescriptor.Singleton<ILoggerProvider, TestLoggerProvider>(),
            };

            var container = new Container();

            container.Configure(x => x.Populate(services));

            var logger = container.GetInstance<ILogger<string>>();

            Assert.NotNull(logger);
            Assert.NotNull(logger.Factory);
            Assert.NotEmpty(logger.Factory.Providers);
        }

        private interface ILoggerProvider { }

        private class TestLoggerProvider : ILoggerProvider { }

        private interface ILoggerFactory
        {
            IEnumerable<ILoggerProvider> Providers { get; }
        }

        private class LoggerFactory : ILoggerFactory
        {
            public LoggerFactory() : this(Enumerable.Empty<ILoggerProvider>())
            {
            }

            public LoggerFactory(IEnumerable<ILoggerProvider> providers)
            {
                Providers = providers.ToArray();
            }

            public IEnumerable<ILoggerProvider> Providers { get; }
        }

        private interface ILogger<T>
        {
            ILoggerFactory Factory { get; }
        }

        private class Logger<T> : ILogger<T>
        {
            public Logger(ILoggerFactory factory)
            {
                Factory = factory;
            }

            public ILoggerFactory Factory { get; }
        }
    }
}
