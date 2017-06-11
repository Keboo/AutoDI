using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoDI.Container.Fody
{
    internal static class ModuleDefinitionMixins
    {
        public static TypeReference Get<T>(this ModuleDefinition module)
        {
            TypeReference rv = module.ImportReference(typeof(T));
            //if (typeof(T).IsGenericType)
            //{
            //    Type[] genericArgs = typeof(T).GetGenericArguments();
            //    if (genericArgs.Any())
            //    {
            //        for(int i = 0; i < genericArgs.Length; i++)
            //            rv.GenericParameters.Add(new GenericParameter(rv));

            //        rv = rv.MakeGenericInstanceType(genericArgs.Select(module.ImportReference)
            //            .ToArray());
            //    }
            //}
            return rv;
        }

        public static TypeDefinition Resolve<T>(this ModuleDefinition module)
        {
            return module.Get<T>().Resolve();
        }
    }
}