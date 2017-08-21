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
            var methods = typeof(ContainerMap_old).GetRuntimeMethods().ToList();
            MakeLazyMethod = methods.Single(m => m.Name == nameof(MakeLazy));
            MakeFuncMethod = methods.Single(m => m.Name == nameof(MakeFunc));
        }

        public void Add(ServiceDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            _accessors[descriptor.ServiceType] = new DelegateContainer(descriptor);
        }

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
                return container.Get(null);
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

        private Lazy<T> MakeLazy<T>() => new Lazy<T>(Get<T>);

        private Func<T> MakeFunc<T>() => () => Get<T>();

        private class DelegateContainer
        {
            private readonly Func<IServiceProvider, object> _get;
            public Type TargetType { get; }
            public Lifetime Lifetime { get; }

            public DelegateContainer(ServiceDescriptor descriptor)
            {
                AutoDIServiceDescriptor autoDIDescriptor = descriptor as AutoDIServiceDescriptor;
                Lifetime = autoDIDescriptor?.AutoDILifetime ?? descriptor.Lifetime.ToAutoDI();

                if (descriptor.ImplementationType != null)
                {
                    //TODO
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    var factory = descriptor.ImplementationFactory;
                    _get = provider => factory(provider); //TODO
                }
                else
                {
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