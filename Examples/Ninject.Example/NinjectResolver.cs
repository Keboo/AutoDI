using System;
using AutoDI;

namespace Ninject.Example
{
    public class NinjectResolver : IDependencyResolver
    {
        private readonly IKernel _kernel;

        public NinjectResolver(IKernel kernel)
        {
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));
            _kernel = kernel;
        }

        public T Resolve<T>(params object[] parameters)
        {
            return _kernel.Get<T>();
        }
    }
}