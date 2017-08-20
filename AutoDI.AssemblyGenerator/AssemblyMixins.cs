using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace AutoDI.AssemblyGenerator
{
    public static class AssemblyMixins
    {
        public static object GetStaticProperty<TContainingType>(this Assembly assembly, string propertyName, Type containerType = null) where TContainingType : class
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            string typeName = TypeMixins.GetTypeName(typeof(TContainingType), containerType);
            Type type = assembly.GetType(typeName);
            if (type == null)
                throw new AssemblyGetPropertyException($"Could not find '{typeof(TContainingType).FullName}' in '{assembly.FullName}'");

            PropertyInfo property = type.GetProperty(propertyName);
            if (property == null)
                throw new AssemblyGetPropertyException($"Could not find property '{propertyName}' on type '{type.FullName}'");

            if (property.GetMethod == null)
                throw new AssemblyGetPropertyException($"Property '{type.FullName}.{propertyName}' does not have a getter");

            if (!property.GetMethod.IsStatic)
                throw new AssemblyGetPropertyException($"Property '{type.FullName}.{propertyName}' is not static");

            return property.GetValue(null);
        }

        public static object InvokeStatic<TContainingType>(this Assembly assembly, string methodName, params object[] parameters) where TContainingType : class
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            Type type = assembly.GetType(typeof(TContainingType).FullName);
            if (type == null)
                throw new AssemblyInvocationExcetion($"Could not find '{typeof(TContainingType).FullName}' in '{assembly.FullName}'");

            MethodInfo method = type.GetMethod(methodName);
            if (method == null)
                throw new AssemblyInvocationExcetion($"Could not find method '{methodName}' on type '{type.FullName}'");

            if (!method.IsStatic)
                throw new AssemblyInvocationExcetion($"Method '{type.FullName}.{methodName}' is not static");

            return method.Invoke(null, parameters);
        }

        public static void InvokeEntryPoint(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            assembly.EntryPoint.Invoke(null, new object[assembly.EntryPoint.GetParameters().Length]);
        }

        public static object InvokeGeneric<TGeneric>(this Assembly assembly, object target, string methodName, params object[] parameters)
        {
            Type genericType = assembly.GetType(typeof(TGeneric).FullName);
            if (genericType == null)
                throw new AssemblyInvocationExcetion($"Could not find generic parameter type '{typeof(TGeneric).FullName}' in '{assembly.FullName}'");

            return InvokeGeneric(assembly, genericType, target, methodName, parameters);
        }

        public static object InvokeGeneric(this Assembly assembly, Type genericType, object target, string methodName, params object[] parameters)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (genericType == null) throw new ArgumentNullException(nameof(genericType));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            Type targetType = target.GetType();

            IEnumerable<MethodInfo> methods = targetType.GetRuntimeMethods().Where(x => x.Name == methodName)
                .Union(targetType.GetInterfaces()
                    .SelectMany(@interface => @interface.GetRuntimeMethods().Where(x => x.Name == methodName)));

            MethodInfo method = methods.FirstOrDefault(m => !m.IsStatic && m.IsGenericMethodDefinition);
            if (method == null)
                throw new AssemblyInvocationExcetion($"Could not find method '{methodName}' on type '{targetType.FullName}'");
            
            MethodInfo genericMethod = method.MakeGenericMethod(genericType);

            return genericMethod.Invoke(target, parameters);
        }

        public static object CreateInstance<T>(this Assembly assembly, Type containerType = null)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            string typeName = TypeMixins.GetTypeName(typeof(T), containerType);
            Type type = assembly.GetType(typeName);
            if (type == null)
                throw new AssemblyCreateInstanceException($"Could not find '{typeName}' in '{assembly.FullName}'");

            foreach (ConstructorInfo ctor in type.GetConstructors().OrderBy(c => c.GetParameters().Length))
            {
                var parameters = ctor.GetParameters();
                if (parameters.All(pi => pi.HasDefaultValue))
                    return ctor.Invoke(parameters.Select(x => x.DefaultValue).ToArray());
            }
            throw new AssemblyCreateInstanceException($"Could not find valid constructor for '{typeof(T).FullName}'");
        }

        public static object Resolve<T>(this Assembly assembly, Type containerType = null)
        {
            throw new NotImplementedException();
            //if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            //
            //Type resolverType = assembly.GetType("AutoDI.AutoDIContainer");
            //if (resolverType == null)
            //    throw new InvalidOperationException("Could not find AutoDIContainer");
            //
            //var resolver = Activator.CreateInstance(resolverType) as IDependencyResolver;
            //
            //if (resolver == null)
            //    throw new InvalidOperationException($"Failed to create resolver '{resolverType.FullName}'");
            //
            //string genericTypeName = TypeMixins.GetTypeName(typeof(T), containerType);
            //Type genericType = assembly.GetType(genericTypeName);
            //if (genericType == null)
            //    throw new AssemblyInvocationExcetion($"Could not find '{genericTypeName}' in '{assembly.FullName}'");
            //
            //return assembly.InvokeGeneric(genericType, resolver, nameof(IDependencyResolver.Resolve), (object) new object[0]);
        }

        public static Assembly SingleAssembly(this IDictionary<string, AssemblyInfo> assemblies)
        {
            return assemblies?.Select(x => x.Value.Assembly).Single();
        }
    }
}