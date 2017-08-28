using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    internal class AutoDIServiceProviderFactory : IServiceProviderFactory<IContainer>
    {
        public IContainer CreateBuilder(IServiceCollection services)
        {
            var map = new ContainerMap();
            
            map.Add(services);

            return map;
        }

        public IServiceProvider CreateServiceProvider(IContainer containerBuilder)
        {
            return new AutoDIServiceProvider(containerBuilder);
        }
    }
}