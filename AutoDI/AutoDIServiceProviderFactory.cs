using Microsoft.Extensions.DependencyInjection;

namespace AutoDI;

internal class AutoDIServiceProviderFactory : IServiceProviderFactory<IContainer>
{
    public IContainer CreateBuilder(IServiceCollection services)
    {
        var map = new ContainerMap
        {
            new AutoDIServiceDescriptor(typeof(IServiceScopeFactory), typeof(AutoDIServiceScopeFactory),
            provider => new AutoDIServiceScopeFactory(provider.GetRequiredService<IContainer>()), Lifetime.Scoped),

            new AutoDIServiceDescriptor(typeof(IServiceProvider), typeof(AutoDIServiceProvider),
            provider => new AutoDIServiceProvider(provider.GetRequiredService<IContainer>()), Lifetime.Scoped)
        };

        map.Add(new AutoDIServiceDescriptor(typeof(IContainer), typeof(ContainerMap), provider => map, Lifetime.Scoped));

        map.Add(services);

        return map;
    }

    public IServiceProvider CreateServiceProvider(IContainer containerBuilder)
    {
        return new AutoDIServiceProvider(containerBuilder);
    }
}