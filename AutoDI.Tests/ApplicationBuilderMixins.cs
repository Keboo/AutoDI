using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AutoDI.Tests
{
    public static class ApplicationBuilderMixins
    {
        public static void WithProvider(this IApplicationBuilder builder, IServiceProvider provider)
        {
            builder.ConfigureServices(sc =>
            {
                var mockFactory = new Mock<IServiceProviderFactory<object>>();
                mockFactory.Setup(x => x.CreateBuilder(It.IsAny<IServiceCollection>()))
                    .Returns(null);
                mockFactory.Setup(x => x.CreateServiceProvider(It.IsAny<object>()))
                    .Returns(provider);
                sc.Add(new ServiceDescriptor(typeof(IServiceProviderFactory<object>), mockFactory.Object));
                sc.Replace(new ServiceDescriptor(typeof(IServiceProvider), provider));
            });
        }
    }
}