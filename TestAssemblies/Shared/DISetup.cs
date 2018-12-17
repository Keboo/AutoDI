using System;
using AutoDI;
using Microsoft.Extensions.DependencyInjection;

namespace AssemblyToProcess
{
    public static class DISetup
    {
        [SetupMethod]
        public static void DoSetup(IApplicationBuilder builder)
        {
            builder.ConfigureServices(c =>
            {
                c.AddAutoDIService(typeof(object), typeof(object), p => new object(), Lifetime.Singleton);
            });
        }
    }
}