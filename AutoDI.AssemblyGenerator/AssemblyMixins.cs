using System;
using System.Linq;
using System.Reflection;

namespace AutoDI.AssemblyGenerator
{
    public static class AssemblyMixins
    {
        public static object InvokeStatic<T>(this Assembly assembly, string methodName, params object[] parameters) where T : class
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            Type type = assembly.GetType(typeof(T).FullName);
            if (type == null)
                throw new AssemblyInvocationExcetion($"Could not find '{typeof(T).FullName}' in '{assembly.FullName}'");

            MethodInfo method = type.GetMethod(methodName);
            if (method == null)
                throw new AssemblyInvocationExcetion($"Could not find method '{methodName}' on type '{type.FullName}'");

            if (!method.IsStatic)
                throw new AssemblyInvocationExcetion($"Method '{type.FullName}.{methodName}' is not static");

            return method.Invoke(null, parameters);
        }

        public static object CreateInstance<T>(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            Type type = assembly.GetType(typeof(T).FullName);
            if (type == null)
                throw new AssemblyCreateInstanceException($"Could not find '{typeof(T).FullName}' in '{assembly.FullName}'");

            foreach (ConstructorInfo ctor in type.GetConstructors().OrderBy(c => c.GetParameters().Length))
            {
                var parameters = ctor.GetParameters();
                if (parameters.All(pi => pi.HasDefaultValue))
                    return ctor.Invoke(parameters.Select(x => x.DefaultValue).ToArray());
            }
            throw new AssemblyCreateInstanceException($"Could not find valid constructor for '{typeof(T).FullName}'");
        }
    }

    public class AssemblyCreateInstanceException : Exception
    {
        public AssemblyCreateInstanceException(string message) 
            : base(message)
        {
            
        }
    }
}