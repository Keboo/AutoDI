using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    internal class AutoDIServiceDescriptor : ServiceDescriptor
    {
        public Lifetime AutoDILifetime { get; }

        public AutoDIServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, Lifetime lifetime)
            : base(serviceType, factory, lifetime.FromAutoDI())
        {
            AutoDILifetime = lifetime;
        }
    }
}