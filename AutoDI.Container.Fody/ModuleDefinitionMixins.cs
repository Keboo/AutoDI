using Mono.Cecil;

namespace AutoDI.Container.Fody
{
    internal static class ModuleDefinitionMixins
    {
        public static TypeReference Get<T>(this ModuleDefinition module)
        {
            TypeReference rv = module.ImportReference(typeof(T));
            return rv;
        }

        public static TypeDefinition Resolve<T>(this ModuleDefinition module)
        {
            return module.Get<T>().Resolve();
        }
    }
}