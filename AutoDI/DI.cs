using System;
using System.Reflection;

namespace AutoDI
{
    public static class DI
    {
        public const string Namespace = "AutoDI";
        public const string TypeName = "<AutoDI>";

        //TODO: Cache type not assembly
        private static Assembly _containerAssembly;
        private static IServiceProvider _global;

        public static IServiceProvider Global
        {
            get
            {
                if (_global == null)
                {
                    Type autoDI = GetAutoDIType(ContainerAssembly);
                    PropertyInfo property = autoDI.GetRuntimeProperty(nameof(Global)) ??
                                            throw new InvalidOperationException($"Could not find {nameof(Global)} property");
                    _global = (IServiceProvider)property.GetValue(null);
                }
                return _global;
            }
        }

        private static Assembly ContainerAssembly
        {
            get
            {
                if (_containerAssembly == null)
                {
                    throw new InvalidOperationException("AutoDI has not been initialized");
                }
                return _containerAssembly;
            }
        }

        public static void Init(Assembly containerAssembly = null, Action<IApplicationBuilder> configureMethod = null)
        {
            Type autoDI = GetAutoDIType(containerAssembly);
            _containerAssembly = autoDI.GetTypeInfo().Assembly;

            var method = autoDI.GetRuntimeMethod(nameof(Init), new[] { typeof(Action<IApplicationBuilder>) });
            if (method == null) throw new InvalidOperationException($"Could not find {nameof(Init)} method on {autoDI.FullName}");
            method.Invoke(null, new object[] { configureMethod });
        }

        public static void Dispose()
        {
            Type autoDI = GetAutoDIType(ContainerAssembly);

            var method = autoDI.GetRuntimeMethod(nameof(Dispose), new Type[0]);
            if (method == null) throw new InvalidOperationException($"Could not find {nameof(Dispose)} method on {autoDI.FullName}");
            method.Invoke(null, new object[0]);
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