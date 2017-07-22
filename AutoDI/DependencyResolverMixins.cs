using System;
using System.Reflection;

namespace AutoDI
{
    public static class DependencyResolverMixins
    {
        public static object InvokeGenericResolve(this IDependencyResolver resolver, Type desiredType, object[] parameters)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (desiredType == null) throw new ArgumentNullException(nameof(desiredType));

            var methodInfo = resolver.GetType().GetRuntimeMethod(nameof(IDependencyResolver.Resolve), new[] { typeof(object[]) });
            methodInfo = methodInfo.MakeGenericMethod(desiredType);
            return methodInfo.Invoke(resolver, new object[] { parameters });
        }
    }
}