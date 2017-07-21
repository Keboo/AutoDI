using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace AutoDI.Fody
{
    internal static class ModuleDefinitionMixins
    {
        public static TypeReference Get<T>(this ModuleDefinition module)
        {
            return module.ImportReference(typeof(T));
        }

        public static TypeDefinition Resolve<T>(this ModuleDefinition module)
        {
            return module.Get<T>().Resolve();
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
    }
}