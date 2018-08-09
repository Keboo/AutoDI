using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoDI
{
    public static class ServiceDescriptorMixins
    {
        public static Lifetime GetAutoDILifetime(this ServiceDescriptor descriptor)
        {
            return (descriptor as AutoDIServiceDescriptor)?.AutoDILifetime ?? descriptor.Lifetime.ToAutoDI();
        }

        public static Type GetTargetType(this ServiceDescriptor serviceDescriptor) =>
                (serviceDescriptor as AutoDIServiceDescriptor)?.TargetType ??
                serviceDescriptor.ImplementationType ??
                serviceDescriptor.ImplementationInstance?.GetType();
    }
}