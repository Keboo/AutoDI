using System;
using System.Linq;
using System.Reflection;

namespace AutoDI.Container.Fody
{
    public static class AutoDIContainer
    {
        public static void Inject(Assembly assembly = null)
        {
            const string typeName = "AutoDI.AutoDIContainer";

            var containerType = Type.GetType(typeName);
            if (containerType == null)
            {
                foreach (Assembly domainAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    containerType = domainAssembly.GetType(typeName);
                    if (containerType != null)
                    {
                        break;
                    }
                }
            }

            if (containerType == null)
                throw new InvalidOperationException("Could not find AutoDI container to inject");
            DependencyResolver.Set((IDependencyResolver)Activator.CreateInstance(containerType));
        }
    }
}