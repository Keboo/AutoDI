
using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWithDependencies
    {
        public IService Service { get; }
        public IService2 Service2 { get; }

        public ClassWithDependencies( [Dependency]IService service = null, [Dependency]IService2 service2 = null )
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (service2 == null) throw new ArgumentNullException(nameof(service2));
            Service = service;
            Service2 = service2;
        }
    }
}
