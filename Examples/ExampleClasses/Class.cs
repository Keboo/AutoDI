using System;
using AutoDI;

namespace ExampleClasses
{
    public class Class
    {
        public IService Service { get; }

        public Class([Dependency]IService service = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Service = service;
        }
    }
}
