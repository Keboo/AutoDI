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

public class AutoDIContainer
{
    public IService Service { get; }
    public IService2 Service2 { get; }

    public AutoDIContainer()
    {
        IDependencyResolver resolver = DependencyResolver.Get();
        if (Service != null)
        {
            Service = resolver.Resolve<IService>();
        }
    }
}