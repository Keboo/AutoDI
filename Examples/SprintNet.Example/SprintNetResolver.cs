using AutoDI;
using Spring.Objects.Factory;
using System;

namespace SprintNet.Example
{
    public class SprintNetResolver : IDependencyResolver
    {
        private readonly IObjectFactory _factory;

        public SprintNetResolver(IObjectFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factory = factory;
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _factory.GetObject<T>();
        }
    }
}