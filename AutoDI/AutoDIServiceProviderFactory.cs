using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    internal class AutoDIServiceProviderFactory : IServiceProviderFactory<ContainerMap>
    {
        public ContainerMap CreateBuilder(IServiceCollection services)
        {
            var map = new ContainerMap();
            
            foreach (ServiceDescriptor serviceDescriptor in services)
            {
                map.Add(serviceDescriptor);
            }

            return map;
        }

        public IServiceProvider CreateServiceProvider(ContainerMap containerBuilder)
        {
            return new AutoDIServiceProvider(containerBuilder);
        }
    }
}