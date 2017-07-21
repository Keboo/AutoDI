using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

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

        //NB: Assumes the base class is System.Object
        public static MethodDefinition CreateDefaultConstructor(this ModuleDefinition module, Type baseType = null)
        {
            var ctor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName, module.ImportReference(typeof(void)));
            ILProcessor processor = ctor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0); //this
            processor.Emit(OpCodes.Call, module.ImportReference((baseType ?? typeof(object)).GetConstructor(new Type[0])));
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