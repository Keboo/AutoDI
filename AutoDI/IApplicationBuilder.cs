using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    public interface IApplicationBuilder
    {
        IApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        IApplicationBuilder ConfigureContinaer<TContainerType>(Action<TContainerType> configureContianer);

        IServiceProvider Build();
    }
}