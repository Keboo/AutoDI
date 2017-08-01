using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    public class AutoDIServiceProvider : IServiceProvider, ISupportRequiredService
    {
        public AutoDIServiceProvider(ContainerMap containerBuilder)
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
            throw new NotImplementedException();
        }

        public IServiceProvider CreateServiceProvider(ContainerMap containerBuilder)
        {
            return new AutoDIServiceProvider(containerBuilder);
        }
    }
}