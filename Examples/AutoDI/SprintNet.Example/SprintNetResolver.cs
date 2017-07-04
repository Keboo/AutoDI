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
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _factory.GetObject<T>();
        }
    }
}