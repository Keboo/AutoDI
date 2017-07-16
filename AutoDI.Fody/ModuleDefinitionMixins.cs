using Mono.Cecil;

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
    }
}