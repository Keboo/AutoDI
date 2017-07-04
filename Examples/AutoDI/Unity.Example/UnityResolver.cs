using System;
using AutoDI;
using Microsoft.Practices.Unity;

namespace Unity.Example
{
    public class UnityResolver : IDependencyResolver
    {
        private readonly IUnityContainer _container;

        public UnityResolver(IUnityContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _container.Resolve<T>();
        }
    }
}