using System;
using System.Reflection;

namespace AutoDI
{
    public static class DI
    {
        public const string Namespace = "AutoDI";
        public const string TypeName = "<AutoDI>";
        public const string GlobalPropertyName = "Global";

        public static void Init(Assembly containerAssembly = null, Action<IApplicationBuilder> configureMethod = null)
        {
            Type autoDI = GetAutoDIType(containerAssembly);

            var method = autoDI.GetRuntimeMethod(nameof(Init), new[] { typeof(Action<IApplicationBuilder>) });
            if (method == null) throw new InvalidOperationException($"Could not find {nameof(Init)} method on {autoDI.FullName}");
            method.Invoke(null, new object[] { configureMethod });
        }

        //TODO: make nullable
        public static void Dispose(Assembly containerAssembly)
        {
            Type autoDI = GetAutoDIType(containerAssembly);

            var method = autoDI.GetRuntimeMethod(nameof(Dispose), new Type[0]);
            if (method == null) throw new InvalidOperationException($"Could not find {nameof(Dispose)} method on {autoDI.FullName}");
            method.Invoke(null, new object[0]);
        }

        public static IServiceProvider GetGlobalServiceProvider(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            Type autoDI = GetAutoDIType(assembly);
            PropertyInfo property = autoDI.GetRuntimeProperty(GlobalPropertyName) ??
                                    throw new InvalidOperationException($"Could not find {GlobalPropertyName} property");
            return (IServiceProvider)property.GetValue(null);
        }

        private static Type GetAutoDIType(Assembly containerAssembly)
        {
            const string typeName = Namespace + "." + TypeName;

            Type containerType = containerAssembly != null
                ? containerAssembly.GetType(typeName)
                : Type.GetType(typeName);

            if (containerType == null)
                throw new InvalidOperationException("Could not find AutoDI class. Was the fody weaver run on this assembly?");
            return containerType;
        }

    }
}