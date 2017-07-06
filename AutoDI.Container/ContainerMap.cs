using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDI.Container
{
    public sealed class ContainerMap
    {
        private readonly Dictionary<Type, DelegateContainer> _accessors = new Dictionary<Type, DelegateContainer>();

        public void AddSingleton<T>(T instance, Type[] keys)
        {
            Add(Lifetime.Singleton, () => instance, keys);
        }

        public void AddLazySingleton<T>(Func<T> create, Type[] keys)
        {
            var lazy = new Lazy<T>(create);
            Add(Lifetime.LazySingleton, () => lazy.Value, keys);
        }

        public void AddWeakTransient<T>(Func<T> create, Type[] keys) where T : class
        {
            var weakRef = new WeakReference<T>(default(T));
            Add(Lifetime.WeakTransient, () =>
            {
                lock (weakRef)
                {
                    if (!weakRef.TryGetTarget(out T value))
                    {
                        value = create();
                        weakRef.SetTarget(value);
                    }
                    return value;
                }
            }, keys);
        }

        public void AddTransient<T>(Func<T> create, Type[] keys)
        {
            Add(Lifetime.Transient, create, keys);
        }

        public T Get<T>()
        {
            if (_accessors.TryGetValue(typeof(T), out DelegateContainer container)
                && (Delegate)container is Func<T> func)
            {
                return func();
            }
            return default(T);
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
                var delegateType = ((Delegate)kvp.Value).GetType();
                var targetType = delegateType.IsConstructedGenericType ? delegateType.GenericTypeArguments.FirstOrDefault() : null;
                yield return new Map(kvp.Key, targetType, kvp.Value.LifetimeMode);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(ContainerMap)} contents:");

            var maps = GetMappings().ToArray();
            int padSize = maps.Select(m => m.SourceType.FullName.Length).Max();

            foreach (Map map in maps)
            {
                sb.AppendLine($"  {map.SourceType.FullName.PadRight(padSize)} -> {map.TargetType?.FullName ?? "<unknown>"} as {map.LifetimeMode}");
            }
            return sb.ToString();
        }

        private abstract class DelegateContainer
        {
            public Lifetime LifetimeMode { get; }

            protected DelegateContainer(Lifetime lifetimeMode)
            {
                LifetimeMode = lifetimeMode;
            }

            public static explicit operator Delegate(DelegateContainer container)
            {
                return container.GetDelegate();
            }

            protected abstract Delegate GetDelegate();
        }

        private class DelegateContainer<T> : DelegateContainer
        {
            private readonly Func<T> _func;

            public DelegateContainer(Func<T> func, Lifetime lifetimeMode) : base(lifetimeMode)
            {
                _func = func;
            }

            protected override Delegate GetDelegate() => _func;
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