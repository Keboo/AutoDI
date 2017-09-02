using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    internal class AutoDIServiceDescriptor : ServiceDescriptor
    {
        public Lifetime AutoDILifetime { get; }

        public Type TargetType { get; }

        public AutoDIServiceDescriptor(Type serviceType, Type targetType, Func<IServiceProvider, object> factory, Lifetime lifetime)
            : base(serviceType, factory, lifetime.FromAutoDI())
        {
            AutoDILifetime = lifetime;
            TargetType = targetType;
        }

        public AutoDIServiceDescriptor(Type serviceType, Type targetType, Lifetime lifetime)
            : base(serviceType, targetType, lifetime.FromAutoDI())
        {
            AutoDILifetime = lifetime;
            TargetType = targetType;
        }
    }
}