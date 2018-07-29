using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    public static class ServiceDescriptorMixins
    {
        public static Lifetime GetAutoDILifetime(this ServiceDescriptor descriptor)
        {
            return (descriptor as AutoDIServiceDescriptor)?.AutoDILifetime ?? descriptor.Lifetime.ToAutoDI();
        }
    }
}