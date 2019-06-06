using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoDI
{
    public static class GlobalDI
    {
        private static readonly List<IServiceProvider> _providers = new List<IServiceProvider>();

        public static IReadOnlyList<IServiceProvider> Providers => _providers.AsReadOnly();

        public static T GetService<T>(object[] parameters)
        {
            lock (_providers)
            {
                if (_providers.Count == 0)
                    throw new NotInitializedException();

                return _providers.Select(provider => provider.GetService<T>(parameters))
                    .FirstOrDefault(service => service != null);
            }
        }

        public static T GetService<T>()
        {
            return GetService<T>(new object[] { });
        }

        public static object GetService(Type serviceType, object[] parameters)
        {
            lock(_providers)
            {
                if (_providers.Count == 0)
                    throw new NotInitializedException();

                return _providers.Select(provider => provider.GetService(serviceType, parameters))
                    .FirstOrDefault(service => service != null);
            }
        }

        public static object GetService(Type serviceType)
        {
            return GetService(serviceType, new object[] { });
        }

        public static void Register(IServiceProvider provider)
        {
            lock (_providers)
            {
                _providers.Insert(0, provider);
            }
        }

        public static bool Unregister(IServiceProvider provider)
        {
            lock (_providers)
            {
                return _providers.Remove(provider);
            }
        }
    }
}