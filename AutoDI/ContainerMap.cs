using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoDI
{
    public sealed class ContainerMap : IContainer
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

        public ContainerMap()
        {
            //TODO: This seems to be created more than expected
        }

        public void Add(IServiceCollection services)
        {
            //TODO: This re-grouping seems off somewhere...
            foreach (IGrouping<Type, ServiceDescriptor> serviceDescriptors in from ServiceDescriptor service in services
                let autoDIService = service as AutoDIServiceDescriptor
                group service by autoDIService?.TargetType
                into @group
                select @group
            )
            {
                DelegateContainer container = null;
                foreach (ServiceDescriptor serviceDescriptor in serviceDescriptors)
                {
                    //Build up the container if it has not been generated or we do not have multiple AutoDI target types
                    if (container == null || serviceDescriptors.Key == null)
                    {
                        AutoDIServiceDescriptor autoDIDescriptor = serviceDescriptor as AutoDIServiceDescriptor;
                        Lifetime lifetime = autoDIDescriptor?.AutoDILifetime ?? serviceDescriptor.Lifetime.ToAutoDI();
                        Type targetType = autoDIDescriptor?.TargetType;

                        container = new DelegateContainer(lifetime, targetType, GetFactory(serviceDescriptor));
                    }

                    _accessors[serviceDescriptor.ServiceType] = container;
                }
            }

            Func<IServiceProvider, object> GetFactory(ServiceDescriptor descriptor)
            {
                if (descriptor.ImplementationType != null)
                {
                    //TODO, resolve parameters
                    return sp => Activator.CreateInstance(descriptor.ImplementationType);
                }
                if (descriptor.ImplementationFactory != null)
                {
                    return descriptor.ImplementationFactory;
                }
                //NB: separate the instance from the ServiceDescriptor to avoid capturing both
                object instance = descriptor.ImplementationInstance;
                return _ => instance;
            }
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
                        .Invoke(this, new object[] {provider});
                }
                if (key.GetGenericTypeDefinition() == typeof(Func<>))
                {
                    return MakeFuncMethod.MakeGenericMethod(key.GenericTypeArguments[0])
                        .Invoke(this, new object[] {provider});
                }
            }
            return default(object);
        }

        public IContainer CreatedNestedContainer()
        {
            var rv = new ContainerMap();

            foreach (KeyValuePair<Type, DelegateContainer> kvp in _accessors)
            {
                rv._accessors[kvp.Key] = kvp.Value.ForNestedContainer();
            }

            return rv;
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
            private readonly Func<IServiceProvider, object> _creationFactory;
            private readonly Func<IServiceProvider, object> _factoryWithLifetime;
            public Type TargetType { get; }
            public Lifetime Lifetime { get; }

            //NB: Only used when creating a nested scope
            public DelegateContainer(Lifetime lifetime, Type targetType, Func<IServiceProvider, object> creationFactory)
            {
                Lifetime = lifetime;
                TargetType = targetType;
                _creationFactory = creationFactory;
                _factoryWithLifetime = WithLifetime(creationFactory, lifetime);
            }

            public DelegateContainer ForNestedContainer()
            {
                switch (Lifetime)
                {
                    case Lifetime.Scoped:
                    case Lifetime.WeakTransient:
                        return new DelegateContainer(Lifetime, TargetType, _creationFactory);
                    default:
                        return this;
                }
            }

            public object Get(IServiceProvider provider) => _factoryWithLifetime(provider);

            private static Func<IServiceProvider, object> WithLifetime(Func<IServiceProvider, object> factory,
                Lifetime lifetime)
            {
                switch (lifetime)
                {
                    case Lifetime.Singleton:
                    case Lifetime.LazySingleton:
                    case Lifetime.Scoped:
                    {
                        var syncLock = new object();
                        object value = null;
                        return provider =>
                        {
                            if (value != null) return value;
                            lock (syncLock)
                            {
                                if (value != null) return value;
                                return value = factory(provider);
                            }
                        };
                    }
                    case Lifetime.WeakTransient:
                    {
                        var weakRef = new WeakReference<object>(null);
                        return provider =>
                        {
                            lock (weakRef)
                            {
                                if (!weakRef.TryGetTarget(out object value))
                                {
                                    value = factory(provider);
                                    weakRef.SetTarget(value);
                                }
                                return value;
                            }
                        };
                    }
                    case Lifetime.Transient:
                        return factory;
                    default:
                        throw new InvalidOperationException($"Unknown lifetime '{lifetime}'");
                }
            }
        }
    }
}