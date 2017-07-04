using System;
using System.Linq;
using System.Reflection;

namespace AutoDI.Container
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
            FieldInfo field = containerType.GetRuntimeFields().SingleOrDefault(f => f.Name == "_items");
            if (field == null) throw new InvalidOperationException("Could not find mapping field in container");
            return (ContainerMap) field.GetValue(null);
        }

        private static Type GetContainerType(Assembly containerAssembly)
        {
            const string typeName = "AutoDI.AutoDIContainer";

            Type containerType = containerAssembly != null
                ? containerAssembly.GetType(typeName)
                : Type.GetType(typeName);

            if (containerType == null)
                throw new InvalidOperationException("Could not find AutoDI container to inject");
            return containerType;
        }
    }
}