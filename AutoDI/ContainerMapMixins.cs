using System;
using System.Linq.Expressions;

namespace AutoDI
{
    public static class ContainerMapMixins
    {
        public static void AddSingleton<T>(this ContainerMap map, T instance, Type[] keys)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddSingleton(() => instance, keys);
        }

        public static void AddSingleton<TService, TImplementation>(this ContainerMap map, TImplementation instance)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddSingleton(() => instance, new[] { typeof(TService) });
        }

        public static void AddSingleton<TService, TImplementation>(this ContainerMap map)
        {
            map.AddSingleton<TService, TImplementation>(Activator.CreateInstance<TImplementation>());
        }

        public static void AddSingleton<TService>(this ContainerMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddSingleton(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }

        public static void AddLazySingleton<TService, TImplementation>(this ContainerMap map, Func<TImplementation> factory)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddLazySingleton(factory, new[] { typeof(TService) });
        }

        public static void AddLazySingleton<TService, TImplementation>(this ContainerMap map)
        {
            map.AddLazySingleton<TService, TImplementation>(Activator.CreateInstance<TImplementation>);
        }

        public static void AddLazySingleton<TService>(this ContainerMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddLazySingleton(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }

        public static void AddWeakTransient<TService, TImplementation>(this ContainerMap map, Func<TImplementation> factory) where TImplementation : class
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddWeakTransient(factory, new[] { typeof(TService) });
        }

        public static void AddWeakTransient<TService, TImplementation>(this ContainerMap map) where TImplementation : class
        {
            map.AddWeakTransient<TService, TImplementation>(Activator.CreateInstance<TImplementation>);
        }

        public static void AddWeakTransient<TService>(this ContainerMap map) where TService : class
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddWeakTransient(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }

        public static void AddTransient<TService, TImplementation>(this ContainerMap map, Func<TImplementation> factory)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddTransient(factory, new[] { typeof(TService) });
        }

        public static void AddTransient<TService, TImplementation>(this ContainerMap map)
        {
            map.AddTransient<TService, TImplementation>(Activator.CreateInstance<TImplementation>);
        }

        public static void AddTransient<TService>(this ContainerMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddTransient(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }
    }
}