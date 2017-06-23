using System;
using System.Reflection;

namespace AutoDI.Container.Fody
{
    public static class AutoDIContainer
    {
        public static void Inject(Assembly containerAssembly = null)
        {
            Type containerType = GetContainerType(containerAssembly);
            IDependencyResolver resolver = (IDependencyResolver) Activator.CreateInstance(containerType);
            DependencyResolver.Set(resolver);
        }

        public static ContainerMap GetMap(Assembly containerAssembly = null)
        {
            Type containerType = GetContainerType(containerAssembly);
            FieldInfo field = containerType.GetField("_items", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField);
            if (field == null) throw new InvalidOperationException("Could not find mapping field in container");
            return (ContainerMap) field.GetValue(null);
        }

        private static Type GetContainerType(Assembly containerAssembly)
        {
            const string typeName = "AutoDI.AutoDIContainer";

            //Checks currently executing assembly
            var containerType = Type.GetType(typeName);
            if (containerAssembly != null)
            {
                containerType = containerAssembly.GetType(typeName);
            }
            else if (containerType == null)
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
            return containerType;
        }
    }
}