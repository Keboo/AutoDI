using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoDI
{
    //TODO: Better name

    public sealed class ContainerMap_old
    {
        private static readonly MethodInfo MakeLazyMethod;
        private static readonly MethodInfo MakeFuncMethod;

        static ContainerMap_old()
        {
            var methods = typeof(ContainerMap_old).GetRuntimeMethods().ToList();
            MakeLazyMethod = methods.Single(m => m.Name == nameof(MakeLazy));
            MakeFuncMethod = methods.Single(m => m.Name == nameof(MakeFunc));
        }

        private readonly Dictionary<Type, DelegateContainer> _accessors = new Dictionary<Type, DelegateContainer>();

        public void AddSingleton<T>(Func<T> factory, Type[] keys)
        {
            var lazy = new Lazy<T>(factory);
            Add(Lifetime.Singleton, () => lazy.Value, keys);
        }

        public void AddLazySingleton<T>(Func<T> factory, Type[] keys)
        {
            var lazy = new Lazy<T>(factory);
            Add(Lifetime.LazySingleton, () => lazy.Value, keys);
        }

        public void AddWeakTransient<T>(Func<T> factory, Type[] keys) where T : class
        {
            var weakRef = new WeakReference<T>(default(T));
            Add(Lifetime.WeakTransient, () =>
            {
                lock (weakRef)
                {
                    if (!weakRef.TryGetTarget(out T value))
                    {
                        value = factory();
                        weakRef.SetTarget(value);
                    }
                    return value;
                }
            }, keys);
        }

        public void AddTransient<T>(Func<T> factory, Type[] keys)
        {
            Add(Lifetime.Transient, factory, keys);
        }

        public bool Remove<T>()
        {
            bool rv = false; 
            foreach (Type key in _accessors.Keys.ToList())
            {
                if (_accessors[key] is DelegateContainer<T>)
                {
                    rv |= _accessors.Remove(key);
                }
            }
            return rv;
        }
        
        public bool RemoveKey(Type key) => _accessors.Remove(key);

        public T Get<T>()
        {
            //https://github.com/Keboo/DoubleDownWat
            object value = Get(typeof(T));
            if (value is T result)
            {
                return result;
            }
            return default(T);
        }

        public object Get(Type key)
        {
            if (_accessors.TryGetValue(key, out DelegateContainer container))
            {
                return container.Get();
            }
            if (key.IsConstructedGenericType)
            {
                if (key.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    return MakeLazyMethod.MakeGenericMethod(key.GenericTypeArguments[0])
                        .Invoke(this, new object[0]);
                }
                if (key.GetGenericTypeDefinition() == typeof(Func<>))
                {
                    return MakeFuncMethod.MakeGenericMethod(key.GenericTypeArguments[0])
                        .Invoke(this, new object[0]);
                }
            }
            return default(object);
        }

        private void Add<T>(Lifetime lifetimeMode, Func<T> @delegate, Type[] keys)
        {
            foreach (Type key in keys)
            {
                _accessors[key] = new DelegateContainer<T>(@delegate, lifetimeMode);
            }
        }

        public IEnumerable<Map> GetMappings()
        {
            foreach (KeyValuePair<Type, DelegateContainer> kvp in _accessors.OrderBy(kvp => kvp.Key.FullName))
            {
                yield return new Map(kvp.Key, kvp.Value.TargetType, kvp.Value.LifetimeMode);
            }
        }

        /// <summary>
        /// This method is used by AutoDI and not expected to be invoked directly.
        /// </summary>
        public void CreateSingletons()
        {
            foreach (KeyValuePair<Type, DelegateContainer> kvp in _accessors
                .Where(kvp => kvp.Value.LifetimeMode == Lifetime.Singleton))
            {
                //Forces creation of objects.
                kvp.Value.Get();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(ContainerMap_old)} contents:");

            var maps = GetMappings().ToArray();
            int padSize = maps.Select(m => m.SourceType.FullName.Length).Max();

            foreach (Map map in maps)
            {
                sb.AppendLine($"  {map.SourceType.FullName.PadRight(padSize)} -> {map.TargetType?.FullName ?? "<unknown>"} as {map.LifetimeMode}");
            }
            return sb.ToString();
        }

        private Lazy<T> MakeLazy<T>() => new Lazy<T>(Get<T>);

        private Func<T> MakeFunc<T>() => () => Get<T>();

        private abstract class DelegateContainer
        {
            public Lifetime LifetimeMode { get; }

            public abstract Type TargetType { get; }

            protected DelegateContainer(Lifetime lifetimeMode)
            {
                LifetimeMode = lifetimeMode;
            }

            public abstract object Get();
        }

        private class DelegateContainer<T> : DelegateContainer
        {
            private readonly Func<T> _func;

            public DelegateContainer(Func<T> func, Lifetime lifetimeMode) : base(lifetimeMode)
            {
                _func = func;
            }

            public override object Get() => _func();
            public override Type TargetType => typeof(T);
        }

        public class Map
        {
            public Type SourceType { get; }
            public Type TargetType { get; }
            public Lifetime LifetimeMode { get; }

            internal Map(Type sourceType, Type targetType, Lifetime lifetimeMode)
            {
                SourceType = sourceType;
                TargetType = targetType;
                LifetimeMode = lifetimeMode;
            }

            public override string ToString()
            {
                return $"{SourceType.FullName} -> {TargetType.FullName} ({LifetimeMode})";
            }
        }
    }
}