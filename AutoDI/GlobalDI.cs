using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    public static class GlobalDI
    {
        private static readonly List<IServiceProvider> Providers = new List<IServiceProvider>();

        public static T GetService<T>(object[] parameters)
        {
            lock (Providers)
            {
                if (Providers.Count == 0)
                    throw new NotInitializedException();

                return Providers.Select(provider => provider.GetService<T>())
                    .FirstOrDefault(service => service != null);
            }
        }

        public static void Register(IServiceProvider provider)
        {
            lock (Providers)
            {
                Providers.Add(provider);
            }
        }

        public static bool Unregister(IServiceProvider provider)
        {
            lock (Providers)
            {
                return Providers.Remove(provider);
            }
        }
    }
}