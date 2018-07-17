using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoDI
{
    public sealed class ContainerMap : IContainer
    {
        public event EventHandler<TypeKeyNotFoundEventArgs> TypeKeyNotFound;

        private static readonly MethodInfo MakeLazyMethod;
        private static readonly MethodInfo MakeFuncMethod;

        private readonly Dictionary<Type, DelegateContainer> _accessors = new Dictionary<Type, DelegateContainer>();

        static ContainerMap()
        {
            var methods = typeof(ContainerMap).GetRuntimeMethods().ToList();
            MakeLazyMethod = methods.Single(m => m.Name == nameof(MakeLazy));
            MakeFuncMethod = methods.Single(m => m.Name == nameof(MakeFunc));
        }

        public void Add(IServiceCollection services)
        {
            //TODO: This re-grouping seems off somewhere...
            foreach (IGrouping<Type, ServiceDescriptor> serviceDescriptors in
                        from ServiceDescriptor service in services
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
                        container = AddInternal(serviceDescriptor);
                    }

                    _accessors[serviceDescriptor.ServiceType] = container;
                }
            }
        }

        public void Add(ServiceDescriptor serviceDescriptor)
        {
            AddInternal(serviceDescriptor);
        }

        private DelegateContainer AddInternal(ServiceDescriptor serviceDescriptor)
        {
            var container = new DelegateContainer(serviceDescriptor);

            _accessors[serviceDescriptor.ServiceType] = container;

            return container;
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
            if (TryGet(key, provider, out object result))
            {
                return result;
            }

            //Type key not found
            var args = new TypeKeyNotFoundEventArgs(key);
            TypeKeyNotFound?.Invoke(this, args);
            return args.Instance;
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

        public IEnumerator<Map> GetEnumerator() => _accessors.OrderBy(kvp => kvp.Key.FullName)
            .Select(kvp => new Map(kvp.Key, kvp.Value.TargetType, kvp.Value.Lifetime)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

        private bool TryGet(Type key, IServiceProvider provider, out object result)
        {
            if (_accessors.TryGetValue(key, out DelegateContainer container))
            {
                result = container.Get(provider);
                return true;
            }
            if (key.IsConstructedGenericType)
            {
                Type genericType = key.GetGenericTypeDefinition();
                if (genericType == typeof(Lazy<>))
                {
                    result = MakeLazyMethod.MakeGenericMethod(key.GenericTypeArguments[0])
                        .Invoke(this, new object[] { provider });
                    return true;
                }
                if (genericType == typeof(Func<>))
                {
                    result = MakeFuncMethod.MakeGenericMethod(key.GenericTypeArguments[0])
                        .Invoke(this, new object[] { provider });
                    return true;
                }
                if (_accessors.TryGetValue(genericType, out container))
                {
                    DelegateContainer genericContainer = container.As(key.GenericTypeArguments);
                    _accessors.Add(key, genericContainer);
                    result = genericContainer.Get(provider);
                    return true;
                }
            }

            if (key.IsClass && !key.IsAbstract)
            {
                foreach (ConstructorInfo constructor in key.GetConstructors().OrderByDescending(c => c.GetParameters().Length))
                {
                    var parameters = constructor.GetParameters();
                    object[] parameterValues = new object[parameters.Length];
                    bool found = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (!TryGet(parameters[i].ParameterType, provider, out object parametersResult))
                        {
                            found = false;
                            break;
                        }
                        parameterValues[i] = parametersResult;
                    }

                    if (found)
                    {
                        result = constructor.Invoke(parameterValues);
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        private class DelegateContainer
        {
            private readonly ServiceDescriptor _serviceDescriptor;
            private readonly Func<IServiceProvider, object> _creationFactory;
            private readonly Func<IServiceProvider, object> _factoryWithLifetime;
            public Type TargetType { get; }
            public Lifetime Lifetime { get; }

            public DelegateContainer(ServiceDescriptor serviceDescriptor)
                :this(GetLifetime(serviceDescriptor), 
                      GetTargetType(serviceDescriptor), 
                      GetFactory(serviceDescriptor))
            {
                _serviceDescriptor = serviceDescriptor ?? throw new ArgumentNullException(nameof(serviceDescriptor));
            }

            private DelegateContainer(Lifetime lifetime, Type targetType,
                Func<IServiceProvider, object> creationFactory)
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
                    case Lifetime.WeakSingleton:
                        return new DelegateContainer(_serviceDescriptor);
                    default:
                        return this;
                }
            }

            public DelegateContainer As(Type[] genericTypeParameters)
            {
                if (_serviceDescriptor.ImplementationType?.IsGenericTypeDefinition != true)
                {
                    throw new InvalidOperationException(
                        $"Attempted to retrieved closed generic for non-open generic type '{_serviceDescriptor.ImplementationType?.FullName ?? "Unknown"}' using '{_serviceDescriptor.ServiceType.FullName}'");
                }

                Type targetType = _serviceDescriptor.ImplementationType.MakeGenericType(genericTypeParameters);
                var closedGenericDescriptor = new AutoDIServiceDescriptor(_serviceDescriptor.ServiceType, targetType, Lifetime);
                return new DelegateContainer(closedGenericDescriptor);
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
                    case Lifetime.WeakSingleton:
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

            private static Lifetime GetLifetime(ServiceDescriptor serviceDescriptor) =>
                (serviceDescriptor as AutoDIServiceDescriptor)?.AutoDILifetime ??
                serviceDescriptor.Lifetime.ToAutoDI();

            private static Type GetTargetType(ServiceDescriptor serviceDescriptor) =>
                (serviceDescriptor as AutoDIServiceDescriptor)?.TargetType ??
                serviceDescriptor.ImplementationType ??
                serviceDescriptor.ImplementationInstance?.GetType();

            private static Func<IServiceProvider, object> GetFactory(ServiceDescriptor descriptor)
            {
                if (descriptor.ImplementationType != null)
                {
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
    }
}