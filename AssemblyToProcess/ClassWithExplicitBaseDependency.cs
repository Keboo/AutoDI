using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWithExplicitBaseDependency : ClassWithExplicitDependency
    {
        public ClassWithExplicitBaseDependency([Dependency]IService service = null) 
            : base(service)
        { }
    }

    public class ClassWithExplicitDependency
    {
        public IService Service { get; }

        public ClassWithExplicitDependency(IService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Service = service;
        }
    }
}