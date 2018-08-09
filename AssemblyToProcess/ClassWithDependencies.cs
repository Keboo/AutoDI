
using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWithDependencies
    {
        public IService Service { get; }
        public IService2 Service2 { get; }

        [Dependency]
        public IService3 Service3 { get; }

        public ClassWithDependencies([Dependency]IService service = null, [Dependency]IService2 service2 = null)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
            Service2 = service2 ?? throw new ArgumentNullException(nameof(service2));
        }
    }
}
