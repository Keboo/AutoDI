using System;
using System.Collections.Generic;

namespace AutoDI.Container.Fody
{
    public sealed class InternalMap
    {
        private readonly Dictionary<Type, Delegate> _accessors = new Dictionary<Type, Delegate>();

        public void AddSingleton<T>(T instance, Type[] keys)
        {
            Add(() => instance, keys);
        }

        public void AddLazySingleton<T>(Func<T> create, Type[] keys)
        {
            var lazy = new Lazy<T>(create);
            Add(() => lazy.Value, keys);
        }

        public void AddWeakTransient<T>(Func<T> create, Type[] keys) where T : class
        {
            var weakRef = new WeakReference<T>(default(T));
            Add(() =>
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
            Add(create, keys);
        }

        public T Get<T>()
        {
            if (_accessors.TryGetValue(typeof(T), out Delegate d)
                && d is Func<T> func)
            {
                return func();
            }
            return default(T);
        }

        private void Add<T>(Func<T> @delegate, Type[] keys)
        {
            foreach (Type key in keys)
            {
                _accessors[key] = @delegate;
            }
        }
    }
}