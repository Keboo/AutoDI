using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDI.Container.Fody
{
    public sealed class ContainerMap
    {
        private readonly Dictionary<Type, DelegateContainer> _accessors = new Dictionary<Type, DelegateContainer>();

        public void AddSingleton<T>(T instance, Type[] keys)
        {
            Add(Create.Singleton, () => instance, keys);
        }

        public void AddLazySingleton<T>(Func<T> create, Type[] keys)
        {
            var lazy = new Lazy<T>(create);
            Add(Create.LazySingleton, () => lazy.Value, keys);
        }

        public void AddWeakTransient<T>(Func<T> create, Type[] keys) where T : class
        {
            var weakRef = new WeakReference<T>(default(T));
            Add(Create.WeakTransient, () =>
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
            Add(Create.Transient, create, keys);
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

        private void Add<T>(Create createMode, Func<T> @delegate, Type[] keys)
        {
            foreach (Type key in keys)
            {
                _accessors[key] = new DelegateContainer<T>(@delegate, createMode);
            }
        }

        public IEnumerable<Map> GetMappings()
        {
            foreach (KeyValuePair<Type, DelegateContainer> kvp in _accessors.OrderBy(kvp => kvp.Key.FullName))
            {
                var delegateType = ((Delegate)kvp.Value).GetType();
                var targetType = delegateType.IsGenericType ? delegateType.GenericTypeArguments.FirstOrDefault() : null;
                yield return new Map(kvp.Key, targetType, kvp.Value.CreateMode);
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
                sb.AppendLine($"  {map.SourceType.FullName.PadRight(padSize)} -> {map.TargetType?.FullName ?? "<unknown>"} as {map.CreateMode}");
            }
            return sb.ToString();
        }

        private abstract class DelegateContainer
        {
            public Create CreateMode { get; }

            protected DelegateContainer(Create createMode)
            {
                CreateMode = createMode;
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

            public DelegateContainer(Func<T> func, Create createMode) : base(createMode)
            {
                _func = func;
            }

            protected override Delegate GetDelegate() => _func;
        }

        public class Map
        {
            public Type SourceType { get; }
            public Type TargetType { get; }
            public Create CreateMode { get; }

            internal Map(Type sourceType, Type targetType, Create createMode)
            {
                SourceType = sourceType;
                TargetType = targetType;
                CreateMode = createMode;
            }

            public override string ToString()
            {
                return $"{SourceType.FullName} -> {TargetType.FullName} ({CreateMode})";
            }
        }

    }
}