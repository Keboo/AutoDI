using System;
using AutoDI;

namespace ExampleLib
{
    public class Manager
    {
        public IService Service { get; }

        public Manager([Dependency]IService service = null)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }
}
