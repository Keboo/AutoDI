using System;
using AutoDI;

namespace AssemblyToProcess
{
    public static class DISetup
    {
        [SetupMethod]
        public static void DoSetup(IApplicationBuilder builder)
        {
            builder.ConfigureServices(c =>
            {
                c.AddAutoDIService<object>(p => new object(), new Type[] {typeof(object)}, Lifetime.Singleton);
            });
        }
    }
}