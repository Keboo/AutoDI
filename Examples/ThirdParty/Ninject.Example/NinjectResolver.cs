using System;
using AutoDI;

namespace Ninject.Example
{
    public class NinjectResolver : IDependencyResolver
    {
        private readonly IKernel _kernel;

        public NinjectResolver(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _kernel.Get<T>();
        }

        public object Resolve(Type desiredType, params object[] parameters)
        {
            return _kernel.Get(desiredType);
        }
    }
}