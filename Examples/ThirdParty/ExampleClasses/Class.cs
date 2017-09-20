using System;
using AutoDI;

namespace ExampleClasses
{
    public class Class
    {
        public IService Service { get; }

        public Class([Dependency]IService service = null)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }
}
