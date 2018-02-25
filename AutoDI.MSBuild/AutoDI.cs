using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI.Generated
{
    public static class AutoDI
    {
        private static object generated_0(IServiceProvider serviceProvider)
        {
            return null;
        }
        public static void AddServices(IServiceCollection collection)
        {
            collection.AddAutoDIService<object>(new Func<IServiceProvider, object>(generated_0), new Type[]
            {
                typeof(object)
            }, Lifetime.Singleton);
        }
    }
}