using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    [DebuggerDisplay("Lifetime = {AutoDILifetime}, ServiceType = {ServiceType}, TargetType = {TargetType}, ImplementationType = {ImplementationType}")]
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