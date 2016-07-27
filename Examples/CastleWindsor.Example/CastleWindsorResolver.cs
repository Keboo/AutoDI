using System;
using AutoDI;
using Castle.Windsor;

namespace CastleWindsor.Example
{
    public class CastleWindsorResolver : IDependencyResolver
    {
        private readonly IWindsorContainer _container;

        public CastleWindsorResolver(IWindsorContainer container)
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