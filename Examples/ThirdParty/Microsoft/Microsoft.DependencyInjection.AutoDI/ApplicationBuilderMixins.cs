using AutoDI;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DependencyInjection.AutoDI
{
    public static class ApplicationBuilderMixins
    {
        public static IApplicationBuilder UseMicrosoftDI(this IApplicationBuilder builder)
        {
            return builder.ConfigureServices(services => services
                .AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>());
        }
    }
}
