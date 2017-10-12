using System;
using System.Collections.Generic;
using System.Linq;

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

                return Providers.Select(provider => provider.GetService<T>(parameters))
                    .FirstOrDefault(service => service != null);
            }
        }

        public static T GetService<T>()
        {
            return GetService<T>(new object[] { });
        }

        public static object GetService(Type serviceType, object[] parameters)
        {
            lock(Providers)
            {
                if (Providers.Count == 0)
                    throw new NotInitializedException();

                return Providers.Select(provider => provider.GetService(serviceType, parameters))
                    .FirstOrDefault(service => service != null);
            }
        }

        public static object GetService(Type serviceType)
        {
            return GetService(serviceType, new object[] { });
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