using System;
using AutoDI;

namespace Autofac.Example
{
    public class AutofacResolver : IDependencyResolver
    {
        private readonly IContainer _container;

        public AutofacResolver(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _container.Resolve<T>();
        }

        public object Resolve(Type desiredType, params object[] parameters)
        {
            return _container.Resolve(desiredType);
        }
    }
}