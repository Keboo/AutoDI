using System;
using AutoDI;

namespace Autofac.Example
{
    public class AutofacResolver : IDependencyResolver
    {
        private readonly IContainer _container;

        public AutofacResolver(IContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            _container = container;
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _container.Resolve<T>();
        }
    }
}