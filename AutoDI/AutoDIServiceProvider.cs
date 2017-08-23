using System;

namespace AutoDI
{
    internal class AutoDIServiceProvider : IServiceProvider, IAutoDISerivceProvider, IInitializeServiceProvider
    {
        internal ContainerMap ContainerMap { get; }

        public AutoDIServiceProvider(ContainerMap containerMap)
        {
            ContainerMap = containerMap ?? throw new ArgumentNullException(nameof(containerMap));
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            return ContainerMap.Get(serviceType, this);
        }

        object IAutoDISerivceProvider.GetService(Type serviceType, object[] parameters)
        {
            //TODO: use parameters
            return ContainerMap.Get(serviceType, this);
        }

        void IInitializeServiceProvider.Initialize()
        {
            ContainerMap.CreateSingletons(this);
        }
    }
}