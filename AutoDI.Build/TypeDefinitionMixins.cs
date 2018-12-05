using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoDI.Build
{
    internal static class TypeDefinitionMixins
    {
        public static bool IsCompilerGenerated(this TypeDefinition type)
        {
            return type.HasCustomAttributes &&
                   type.CustomAttributes.Any(a => string.Equals(a.AttributeType.FullName,
                       typeof(CompilerGeneratedAttribute).FullName));
        }

        //Issue 75
        public static bool CanMapType(this TypeDefinition type)
        {
            while (true)
            {
                if (!type.IsNested)
                {
                    return true;
                }

                //public, protected internal, and internal
                if (!type.IsNestedPublic && !type.IsNestedFamilyOrAssembly && !type.IsNestedAssembly)
                {
                    return false;
                }

                type = type.DeclaringType;
            }
        }

        public static MethodDefinition GetMappingConstructor(this TypeDefinition targetType)
        {
            var targetTypeCtors = targetType.GetConstructors();
            var annotatedConstructors = targetTypeCtors
                .Where(ctor => ctor.CustomAttributes.Any(attr => attr.AttributeType.FullName == "AutoDI.DiConstructorAttribute")).ToArray();
            MethodDefinition targetTypeCtor;

            if (annotatedConstructors.Length > 0)
            {
                if (annotatedConstructors.Length > 1)
                {
                    throw new MultipleConstructorException($"More then one constructor on '{targetType.Name}' annotated with DiConstructorAttribute");
                }
                targetTypeCtor = annotatedConstructors[0];
            }
            else
            {
                targetTypeCtor = targetType.GetConstructors().OrderByDescending(c => c.Parameters.Count)
                    .FirstOrDefault();
            }

            return targetTypeCtor;
        }
    }
}