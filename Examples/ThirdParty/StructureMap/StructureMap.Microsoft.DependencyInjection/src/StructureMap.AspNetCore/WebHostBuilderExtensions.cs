using Microsoft.AspNetCore.Hosting;

namespace StructureMap.AspNetCore
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseStructureMap(this IWebHostBuilder builder)
        {
            return UseStructureMap(builder, registry: null);
        }

        public static IWebHostBuilder UseStructureMap(this IWebHostBuilder builder, Registry registry)
        {
            return builder.ConfigureServices(services => services.AddStructureMap(registry));
        }
    }
}