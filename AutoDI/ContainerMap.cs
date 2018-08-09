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

        private readonly Dictionary<Type, IDelegateContainer> _accessors = new Dictionary<Type, IDelegateContainer>();

        static ContainerMap()
        {
            var methods = typeof(ContainerMap).GetRuntimeMethods().ToList();
            MakeLazyMethod = methods.Single(m => m.Name == nameof(MakeLazy));
            MakeFuncMethod = methods.Single(m => m.Name == nameof(MakeFunc));
        }

        public void Add(IServiceCollection services)
        {
            var factories = new Dictionary<(Type, Lifetime), Func<IServiceProvider, object>>();
            //NB: Order of items in the collection matter (last in wins)
            foreach (ServiceDescriptor serviceDescriptor in services)
            {
                Type targetType = serviceDescriptor.GetTargetType();
                Lifetime lifetime = serviceDescriptor.GetAutoDILifetime();
                if (targetType == null || !factories.TryGetValue((targetType, lifetime), out Func<IServiceProvider, object> factory))
                {
                    factory = GetFactory(serviceDescriptor, lifetime);
                    if (targetType != null)
                    {
                        factories[(targetType, lifetime)] = factory;
                    }
                }
                AddInternal(new DelegateContainer(serviceDescriptor, factory), serviceDescriptor.ServiceType);
            }
        }

        public void Add(ServiceDescriptor serviceDescriptor)
        {
            AddInternal(new DelegateContainer(serviceDescriptor), serviceDescriptor.ServiceType);
        }

        private void AddInternal(DelegateContainer container, Type key)
        {
            if (_accessors.TryGetValue(key, out IDelegateContainer existing))
            {
                _accessors[key] = existing + container;
            }
            else
            {
                _accessors[key] = container;
            }
        }

        public bool Remove<T>() => Remove(typeof(T));

        public bool Remove(Type serviceType) => _accessors.Remove(serviceType);

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

        public object Get(Type serviceType, IServiceProvider provider)
        {
            if (TryGet(serviceType, provider ?? new ContainerServiceProvider(this), out object result))
            {
                return result;
            }

            //Type key not found
            var args = new TypeKeyNotFoundEventArgs(serviceType);
            TypeKeyNotFound?.Invoke(this, args);
            return args.Instance;
        }

        public IContainer CreatedNestedContainer()
        {
            var rv = new ContainerMap();

            foreach (KeyValuePair<Type, IDelegateContainer> kvp in _accessors)
            {
                rv._accessors[kvp.Key] = kvp.Value.ForNestedContainer();
            }

            return rv;
        }

        public IEnumerator<Map> GetEnumerator() => _accessors.OrderBy(kvp => kvp.Key.FullName)
            .SelectMany(kvp => kvp.Value.GetMaps(kvp.Key)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// This method is used by AutoDI and not expected to be invoked directly.
        /// </summary>
        internal void CreateSingletons(IServiceProvider provider)
        {
            foreach (IDelegateContainer container in _accessors.Values)
            {
                container.InstantiateSingletons(provider);
            }
        }

        private Lazy<T> MakeLazy<T>(IServiceProvider provider) => new Lazy<T>(() => Get<T>(provider));

        private Func<T> MakeFunc<T>(IServiceProvider provider) => () => Get<T>(provider);

        private bool TryGet(Type key, IServiceProvider provider, out object result)
        {
            if (_accessors.TryGetValue(key, out IDelegateContainer container))
            {
                result = container.Get(provider);
                return true;
            }

            if (key.IsArray &&
                key.GetElementType() is Type elementType &&
                _accessors.TryGetValue(elementType, out container))
            {
                result = container.GetArray(elementType, provider);
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

                if (genericType == typeof(IEnumerable<>) &&
                    _accessors.TryGetValue(key.GenericTypeArguments[0], out container))
                {
                    result = container.GetArray(key.GenericTypeArguments[0], provider);
                    return true;
                }
                if (_accessors.TryGetValue(genericType, out container))
                {
                    IDelegateContainer genericContainer = container.AsGeneric(key.GenericTypeArguments);
                    if (genericContainer != null)
                    {
                        _accessors.Add(key, genericContainer);
                        result = genericContainer.Get(provider);
                        return true;
                    }
                }

            }

            return TryCreate(key, provider, out result);
        }

        private static bool TryCreate(Type desiredType, IServiceProvider provider, out object result)
        {
            if (desiredType.IsClass && !desiredType.IsAbstract)
            {
                foreach (ConstructorInfo constructor in desiredType.GetConstructors().OrderByDescending(c => c.GetParameters().Length))
                {
                    var parameters = constructor.GetParameters();
                    object[] parameterValues = new object[parameters.Length];
                    bool found = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        parameterValues[i] = provider.GetService(parameters[i].ParameterType);
                        if (parameterValues[i] == null)
                        {
                            found = false;
                            break;
                        }
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

        private static Func<IServiceProvider, object> GetFactory(ServiceDescriptor descriptor, Lifetime lifetime)
        {
            return WithLifetime(GetFactoryMethod());

            Func<IServiceProvider, object> GetFactoryMethod()
            {
                if (descriptor.ImplementationType != null)
                {
                    return sp =>
                    {
                        if (TryCreate(descriptor.ImplementationType, sp, out object result))
                        {
                            return result;
                        }
                        return null;
                    };
                }
                if (descriptor.ImplementationFactory != null)
                {
                    return descriptor.ImplementationFactory;
                }
                //NB: separate the instance from the ServiceDescriptor to avoid capturing both
                object instance = descriptor.ImplementationInstance;
                return _ => instance;
            }

            Func<IServiceProvider, object> WithLifetime(Func<IServiceProvider, object> factory)
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
        }

        private interface IDelegateContainer
        {
            void InstantiateSingletons(IServiceProvider provider);
            IEnumerable<Map> GetMaps(Type sourceType);
            IDelegateContainer ForNestedContainer();
            object Get(IServiceProvider provider);
            Array GetArray(Type elementType, IServiceProvider provider);
            IDelegateContainer AsGeneric(Type[] genericTypeArguments);
        }

        private sealed class DelegateContainer : IDelegateContainer
        {
            private readonly ServiceDescriptor _serviceDescriptor;
            private readonly Func<IServiceProvider, object> _factoryWithLifetime;
            private Type TargetType { get; }
            private Lifetime Lifetime { get; }

            public DelegateContainer(ServiceDescriptor serviceDescriptor, Func<IServiceProvider, object> factory)
                : this(serviceDescriptor.GetAutoDILifetime(), serviceDescriptor.GetTargetType(), factory)
            {
                _serviceDescriptor = serviceDescriptor ?? throw new ArgumentNullException(nameof(serviceDescriptor));
            }

            public DelegateContainer(ServiceDescriptor serviceDescriptor)
                : this(serviceDescriptor.GetAutoDILifetime(),
                       serviceDescriptor.GetTargetType(),
                       GetFactory(serviceDescriptor, serviceDescriptor.GetAutoDILifetime()))
            {
                _serviceDescriptor = serviceDescriptor ?? throw new ArgumentNullException(nameof(serviceDescriptor));
            }

            private DelegateContainer(Lifetime lifetime, Type targetType,
                Func<IServiceProvider, object> creationFactory)
            {
                Lifetime = lifetime;
                TargetType = targetType;
                _factoryWithLifetime = creationFactory;
            }

            public void InstantiateSingletons(IServiceProvider provider)
            {
                if (Lifetime == Lifetime.Singleton)
                {
                    Get(provider);
                }
            }

            public IEnumerable<Map> GetMaps(Type sourceType)
            {
                yield return new Map(sourceType, TargetType, Lifetime);
            }

            public IDelegateContainer ForNestedContainer()
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

            public Array GetArray(Type elementType, IServiceProvider provider)
            {
                var array = Array.CreateInstance(elementType, 1);
                array.SetValue(Get(provider), 0);
                return array;
            }

            public IDelegateContainer AsGeneric(Type[] genericTypeParameters)
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

            public static IDelegateContainer operator +(IDelegateContainer left, DelegateContainer right)
            {
                if (left == null) return right;
                if (right == null) return left;

                if (left is MulticastDelegateContainer multicastDelegateContainer)
                {
                    multicastDelegateContainer.Add(right);
                    return multicastDelegateContainer;
                }
                return new MulticastDelegateContainer(left, right);
            }

            private sealed class MulticastDelegateContainer : IDelegateContainer
            {
                private List<IDelegateContainer> Containers { get; }

                public MulticastDelegateContainer(params IDelegateContainer[] containers)
                {
                    Containers = new List<IDelegateContainer>(containers);
                }

                public void Add(IDelegateContainer container)
                {
                    Containers.Add(container);
                }

                public void InstantiateSingletons(IServiceProvider provider)
                {
                    foreach (IDelegateContainer container in Containers)
                    {
                        container.InstantiateSingletons(provider);
                    }
                }

                public IEnumerable<Map> GetMaps(Type sourceType)
                {
                    return Containers.SelectMany(c => c.GetMaps(sourceType));
                }

                public IDelegateContainer ForNestedContainer()
                {
                    return new MulticastDelegateContainer(Containers.Select(x => x.ForNestedContainer()).ToArray());
                }

                public object Get(IServiceProvider provider) => Containers.Last().Get(provider);

                public Array GetArray(Type elementType, IServiceProvider provider)
                {
                    Array[] arrays = Containers.Select(c => c.GetArray(elementType, provider)).ToArray();
                    Array rv = Array.CreateInstance(elementType, arrays.Sum(x => x.Length));
                    int index = 0;
                    foreach (Array array in arrays)
                    {
                        Array.Copy(array, 0, rv, index, array.Length);
                        index += array.Length;
                    }
                    return rv;
                }

                public IDelegateContainer AsGeneric(Type[] genericTypeArguments) => null;
            }
        }

        private class ContainerServiceProvider : IServiceProvider
        {
            private readonly IContainer _container;

            public ContainerServiceProvider(IContainer container)
            {
                _container = container;
            }

            public object GetService(Type serviceType) => _container.Get(serviceType, this);
        }
    }
}