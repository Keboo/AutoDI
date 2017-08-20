using System;

namespace AutoDI
{
    internal class AutoDIServiceProvider : IServiceProvider, IAutoDISerivceProvider
    {
        private readonly ContainerMap _containerMap;

        public AutoDIServiceProvider(ContainerMap containerMap)
        {
            _containerMap = containerMap ?? throw new ArgumentNullException(nameof(containerMap));
        }

        public object GetService(Type serviceType)
        {
            return _containerMap.Get(serviceType);
        }

        public object GetService(Type serviceType, object[] parameters)
        {
            //TODO: use parameters
            return _containerMap.Get(serviceType);
        }
    }
}