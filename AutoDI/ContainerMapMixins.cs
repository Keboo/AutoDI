using System;

namespace AutoDI
{
    public static class ContainerMapMixins
    {
        public static void AddSingleton<T>(this ContainerMap_old map, T instance, Type[] keys)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddSingleton(() => instance, keys);
        }

        public static void AddSingleton<TService, TImplementation>(this ContainerMap_old map, TImplementation instance)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddSingleton(() => instance, new[] { typeof(TService) });
        }

        public static void AddSingleton<TService, TImplementation>(this ContainerMap_old map)
        {
            map.AddSingleton<TService, TImplementation>(Activator.CreateInstance<TImplementation>());
        }

        public static void AddSingleton<TService>(this ContainerMap_old map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddSingleton(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }

        public static void AddLazySingleton<TService, TImplementation>(this ContainerMap_old map, Func<TImplementation> factory)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddLazySingleton(factory, new[] { typeof(TService) });
        }

        public static void AddLazySingleton<TService, TImplementation>(this ContainerMap_old map)
        {
            map.AddLazySingleton<TService, TImplementation>(Activator.CreateInstance<TImplementation>);
        }

        public static void AddLazySingleton<TService>(this ContainerMap_old map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddLazySingleton(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }

        public static void AddWeakTransient<TService, TImplementation>(this ContainerMap_old map, Func<TImplementation> factory) where TImplementation : class
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddWeakTransient(factory, new[] { typeof(TService) });
        }

        public static void AddWeakTransient<TService, TImplementation>(this ContainerMap_old map) where TImplementation : class
        {
            map.AddWeakTransient<TService, TImplementation>(Activator.CreateInstance<TImplementation>);
        }

        public static void AddWeakTransient<TService>(this ContainerMap_old map) where TService : class
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddWeakTransient(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }

        public static void AddTransient<TService, TImplementation>(this ContainerMap_old map, Func<TImplementation> factory)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddTransient(factory, new[] { typeof(TService) });
        }

        public static void AddTransient<TService, TImplementation>(this ContainerMap_old map)
        {
            map.AddTransient<TService, TImplementation>(Activator.CreateInstance<TImplementation>);
        }

        public static void AddTransient<TService>(this ContainerMap_old map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.AddTransient(Activator.CreateInstance<TService>, new[] { typeof(TService) });
        }
    }
}