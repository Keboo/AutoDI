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