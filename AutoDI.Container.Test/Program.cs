using System;
using System.Collections.Generic;
using AutoDI;
using AutoDI.Container.Test;

namespace AutoDI.Container.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            DependencyResolver.Set(new AutoDIContainer());
            var test = new Test();
            test.RunTest();
            Console.ReadLine();
        }

        private class Test
        {
            private readonly IManager _manager;
            public Test([Dependency]IManager manager = null)
            {
                _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            }

            public void RunTest()
            {
                Console.WriteLine($"Manager = {_manager is Manager}");
                Console.WriteLine($"Service1 = {_manager.Service1 is Service1}");
                Console.WriteLine($"Service2 = {_manager.Service2 is Service2}");
            }
        }
    }
}

public class AutoDIContainer : IDependencyResolver
{
    private static readonly Dictionary<Type, Lazy<object>> _items = new Dictionary<Type, Lazy<object>>();

    static AutoDIContainer()
    {
        //var f = new Func<object>(() => new Manager());
        //var l = new Lazy<object>(f);
        _items[typeof(IManager)] = new Lazy<object>(() => new Manager());
        //_objects[typeof(IService)] = new Lazy<object>(() => new Service1());
        //_objects[typeof(IService2)] = new Lazy<object>(() => new Service2());
        //_objects[typeof(object)] = new Lazy<object>(() => Get<object>());
    }

    T IDependencyResolver.Resolve<T>(params object[] parameters) => 
        _items.TryGetValue(typeof(T), out Lazy<object> lazy) ? (T)lazy.Value : default(T);
}
