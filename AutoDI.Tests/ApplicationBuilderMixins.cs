using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoDI.Tests
{
    public static class ApplicationBuilderMixins
    {
        public static void WithProvider(this IApplicationBuilder builder, IServiceProvider provider)
        {
            builder.ConfigureServices(sc => sc.Replace(new ServiceDescriptor(typeof(IServiceProvider), provider)));
        }
    }
}