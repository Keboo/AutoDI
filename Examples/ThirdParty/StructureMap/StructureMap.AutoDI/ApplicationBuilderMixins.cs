using AutoDI;

namespace StructureMap.AutoDI
{
    public static class ApplicationBuilderMixins
    {
        public static IApplicationBuilder UseStructureMap(this IApplicationBuilder builder)
        {
            return UseStructureMap(builder, registry: null);
        }

        public static IApplicationBuilder UseStructureMap(this IApplicationBuilder builder, Registry registry)
        {
            return builder.ConfigureServices(services => services.AddStructureMap(registry));
        }
    }
}
