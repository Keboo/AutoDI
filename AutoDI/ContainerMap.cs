using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoDI
{
    public sealed class ContainerMap
    {
        private static readonly MethodInfo MakeLazyMethod;
        private static readonly MethodInfo MakeFuncMethod;

        private readonly Dictionary<Type, DelegateContainer> _accessors = new Dictionary<Type, DelegateContainer>();

        static ContainerMap()
        {
            var methods = typeof(ContainerMap).GetRuntimeMethods().ToList();
            MakeLazyMethod = methods.Single(m => m.Name == nameof(MakeLazy));
            MakeFuncMethod = methods.Single(m => m.Name == nameof(MakeFunc));
        }

        public void Add(ServiceDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            _accessors[descriptor.ServiceType] = new DelegateContainer(descriptor);
        }

        public T Get<T>(IServiceProvider provider)
        {
            //https://github.com/Keboo/DoubleDownWat
            object value = Get(typeof(T), provider);
            if (value is T result)
            {
                return result;
            }
            return default(T);
        }

        public object Get(Type key, IServiceProvider provider)
        {
            if (_accessors.TryGetValue(key, out DelegateContainer container))
            {
                return container.Get(provider);
            }
            if (key.IsConstructedGenericType)
            {
                if (key.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    return MakeLazyMethod.MakeGenericMethod(key.GenericTypeArguments[0])
                        .Invoke(this, new object[] { provider });
                }
                if (key.GetGenericTypeDefinition() == typeof(Func<>))
                {
                    return MakeFuncMethod.MakeGenericMethod(key.GenericTypeArguments[0])
                        .Invoke(this, new object[] { provider });
                }
            }
            return default(object);
        }

        public IEnumerable<Map> GetMappings()
        {
            foreach (KeyValuePair<Type, DelegateContainer> kvp in _accessors.OrderBy(kvp => kvp.Key.FullName))
            {
                yield return new Map(kvp.Key, kvp.Value.TargetType, kvp.Value.Lifetime);
            }
        }

        /// <summary>
        /// This method is used by AutoDI and not expected to be invoked directly.
        /// </summary>
        internal void CreateSingletons(IServiceProvider provider)
        {
            foreach (KeyValuePair<Type, DelegateContainer> kvp in _accessors
                .Where(kvp => kvp.Value.Lifetime == Lifetime.Singleton))
            {
                //Forces creation of objects.
                kvp.Value.Get(provider);
            }
        }

        private Lazy<T> MakeLazy<T>(IServiceProvider provider) => new Lazy<T>(() => Get<T>(provider));

        private Func<T> MakeFunc<T>(IServiceProvider provider) => () => Get<T>(provider);

        private class DelegateContainer
        {
            private readonly Func<IServiceProvider, object> _get;
            public Type TargetType { get; }
            public Lifetime Lifetime { get; }

            public DelegateContainer(ServiceDescriptor descriptor)
            {
                AutoDIServiceDescriptor autoDIDescriptor = descriptor as AutoDIServiceDescriptor;
                Lifetime = autoDIDescriptor?.AutoDILifetime ?? descriptor.Lifetime.ToAutoDI();
                TargetType = autoDIDescriptor?.TargetType;

                if (descriptor.ImplementationType != null)
                {
                    //TODO
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    Func<IServiceProvider, object> factory = descriptor.ImplementationFactory;
                    _get = factory;
                }
                else
                {
                    //TODO implicit singleton?
                    object instance = descriptor.ImplementationInstance;
                    _get = _ => instance;
                }
            }

            public object Get(IServiceProvider provider) => _get(provider);
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