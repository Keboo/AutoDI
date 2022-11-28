using System.Reflection;

using Microsoft.AspNetCore.Hosting;

[assembly: AutoDI.Settings(InitMode = AutoDI.InitMode.Manual)]

namespace AutoDI.AspNetCore;

public static class WebHostBuilderMixins
{
    public static IWebHostBuilder UseAutoDI(this IWebHostBuilder builder, Assembly? containerAssembly = null)
    {
        return builder.ConfigureServices(services => DI.AddServices(services, containerAssembly ?? Assembly.GetEntryAssembly()));
    }
}