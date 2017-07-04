using System;
using System.Collections.Generic;
using AssemblyToProcess;
using AutoDI;

namespace AssemblyToProcess
{
    public class Manager : IManager
    {
        public IService Service1 { get; }
        public IService2 Service2 { get; }

        public Manager([Dependency]IService service1 = null, [Dependency]IService2 service2 = null)
        {
            Service1 = service1;
            Service2 = service2;
        }
    }

    public interface IManager { }
}

public class AutoDIContainer : IDependencyResolver
{
    private static readonly Dictionary<Type, Lazy<object>> _objects = new Dictionary<Type, Lazy<object>>();

    static AutoDIContainer()
    {
        _objects[typeof(IManager)] = new Lazy<object>(() => new Manager());
        //_objects[typeof(object)] = new Lazy<object>(() => Get<object>());
    }

    T IDependencyResolver.Resolve<T>(params object[] parameters) => Get<T>();

    private static T Get<T>()
    {
        if (_objects.TryGetValue(typeof(T), out Lazy<object> lazy))
        {
            return (T)lazy.Value;
        }
        return default(T);
    }
}