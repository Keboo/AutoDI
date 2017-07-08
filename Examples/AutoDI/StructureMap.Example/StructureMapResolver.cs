using System;
using AutoDI;

namespace StructureMap.Example
{
    public class StructureMapResolver : IDependencyResolver
    {
        private readonly Container _container;

        public StructureMapResolver(Container container)
        {
            _container = container;
            if (container == null) throw new ArgumentNullException(nameof(container));
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _container.GetInstance<T>();
        }
    }
}