using System;
using System.Collections.Generic;

namespace AutoDI.Container.Fody
{
    internal sealed class InternalMap
    {
        private readonly Dictionary<Type, Delegate> _accessors = new Dictionary<Type, Delegate>();

        public void AddSingleton<TKey, TValue>(TValue instance)
        {
            _accessors[typeof(TKey)] = new Func<TValue>(() => instance);
        }

        public void AddLazySingleton<TKey, TValue>(Func<TValue> create)
        {
            var lazy = new Lazy<TValue>(create);
            _accessors[typeof(TKey)] = new Func<TValue>(() => lazy.Value);
        }

        public void AddWeakTransient<TKey, TValue>(Func<TValue> create) where TValue : class
        {
            var weakRef = new WeakReference<TValue>(default(TValue));
            _accessors[typeof(TKey)] = new Func<TValue>(() =>
            {
                lock (weakRef)
                {
                    if (!weakRef.TryGetTarget(out TValue value))
                    {
                        value = create();
                        weakRef.SetTarget(value);
                    }
                    return value;
                }
            });
        }

        public void AddTransient<TKey, TValue>(Func<TValue> create)
        {
            _accessors[typeof(TKey)] = create;
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
    }
}