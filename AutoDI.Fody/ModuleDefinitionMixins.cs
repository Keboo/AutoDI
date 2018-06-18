using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Reflection;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace AutoDI.Fody
{
    internal static class ModuleDefinitionMixins
    {
        public static TypeReference Get<T>(this ModuleDefinition module)
        {
            return module.ImportReference(typeof(T));
        }

        public static MethodDefinition CreateDefaultConstructor(this ModuleDefinition module, Type baseType = null)
        {
            var ctor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName, module.ImportReference(typeof(void)));
            ILProcessor processor = ctor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0); //this

            ConstructorInfo baseCtor = (baseType ?? typeof(object))
                .GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0],
                    null);
            if (baseCtor == null)
                throw new Exception($"Could not find constructor for '{(baseType ?? typeof(object)).FullName}'");
            processor.Emit(OpCodes.Call, module.ImportReference(baseCtor));
            processor.Emit(OpCodes.Ret);
            return ctor;
        }

        internal static MethodDefinition CreateStaticConstructor(this ModuleDefinition module)
        {
            var ctor = new MethodDefinition(".cctor",
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static |
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.ImportReference(typeof(void)));

            return ctor;
        }

        public static FieldDefinition CreateStaticReadonlyField<T>(this ModuleDefinition moduleDefintion, string name, bool @public)
        {
            if (moduleDefintion == null) throw new ArgumentNullException(nameof(moduleDefintion));
            if (name == null) throw new ArgumentNullException(nameof(name));

            return moduleDefintion.CreateStaticReadonlyField(name, @public, moduleDefintion.Get<T>());
        }

        public  static FieldDefinition CreateStaticReadonlyField(this ModuleDefinition moduleDefinition, string name, 
            bool @public, TypeReference type)
        {
            if (moduleDefinition == null) throw new ArgumentNullException(nameof(moduleDefinition));
            if (name == null) throw new ArgumentNullException(nameof(name));

            return new FieldDefinition(name,
                (@public ? FieldAttributes.Public : FieldAttributes.Private) | FieldAttributes.Static |
                FieldAttributes.InitOnly, moduleDefinition.ImportReference(type));
        }

        public static FieldDefinition CreateReadonlyField<T>(this ModuleDefinition moduleDefinition, string name, bool @public)
        {
            if (moduleDefinition == null) throw new ArgumentNullException(nameof(moduleDefinition));
            if (name == null) throw new ArgumentNullException(nameof(name));

            return moduleDefinition.CreateReadonlyField(name, @public, moduleDefinition.Get<T>());
        }

        public static FieldDefinition CreateReadonlyField(this ModuleDefinition moduleDefinition,
            string name, bool @public, TypeReference type)
        {
            if (moduleDefinition == null) throw new ArgumentNullException(nameof(moduleDefinition));
            if (name == null) throw new ArgumentNullException(nameof(name));

            return new FieldDefinition(name,
                (@public ? FieldAttributes.Public : FieldAttributes.Private) |
                FieldAttributes.InitOnly, moduleDefinition.ImportReference(type));
        }

        public static MethodReference GetDefaultConstructor<T>(this ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.GetDefaultConstructor(typeof(T));
        }

        public static MethodReference GetDefaultConstructor(this ModuleDefinition moduleDefinition, Type targetType)
        {
            if (moduleDefinition == null) throw new ArgumentNullException(nameof(moduleDefinition));

            var defualtCtor = targetType.GetConstructor(new Type[0]);
            if (defualtCtor == null)
            {
                throw new InvalidOperationException($"Could not find default constructor for '{targetType.FullName}'");
            }
            return moduleDefinition.ImportReference(defualtCtor);
        }

        public static MethodReference GetConstructor<T>(this ModuleDefinition moduleDefinition, params Type[] argumentTypes)
        {
            return moduleDefinition.GetConstructor(typeof(T), argumentTypes);
        }

        public static MethodReference GetConstructor(this ModuleDefinition moduleDefinition, Type targetType, params Type[] argumentTypes)
        {
            if (moduleDefinition == null) throw new ArgumentNullException(nameof(moduleDefinition));

            var ctors = targetType.GetConstructors();

            if (argumentTypes?.Any() == true)
            {
                ctors = ctors.Where(c => c.GetParameters().Select(x => x.ParameterType).SequenceEqual(argumentTypes))
                    .ToArray();
            }

            switch (ctors.Length)
            {
                case 0:
                    throw new InvalidOperationException($"Could not find any matching constructors for '{targetType.FullName}'");
                case 1:
                    return moduleDefinition.ImportReference(ctors[0]);
                default:
                    throw new InvalidOperationException($"Found multiple matching constructors for '{targetType.FullName}'");
            }
        }

        public static MethodReference GetMethod<TContainingType>(this ModuleDefinition moduleDefinition,
            string methodName)
        {
            return moduleDefinition.GetMethod(typeof(TContainingType), methodName);
        }

        public static MethodReference GetMethod(this ModuleDefinition moduleDefinition,
            Type containingType, string methodName)
        {
            if (moduleDefinition == null) throw new ArgumentNullException(nameof(moduleDefinition));
            if (containingType == null) throw new ArgumentNullException(nameof(containingType));
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            MethodInfo method = containingType.GetMethod(methodName);
            if (method == null) throw new InvalidOperationException($"Could not find method '{methodName}' on '{containingType.FullName}'");

            return moduleDefinition.ImportReference(method);
        }
    }
}