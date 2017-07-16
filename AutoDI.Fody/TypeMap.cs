using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace AutoDI.Fody
{
    internal class TypeMap
    {
        public TypeMap(TypeDefinition targetType)
        {
            TargetType = targetType;
        }

        public Lifetime Lifetime { get; set; } = Lifetime.LazySingleton;

        public TypeDefinition TargetType { get; }

        public ICollection<TypeDefinition> Keys { get; } = new HashSet<TypeDefinition>(TypeComparer.FullName);

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (TypeDefinition key in Keys)
            {
                sb.AppendLine($"{key.FullName} => {TargetType.FullName} ({Lifetime})");
            }
            return sb.ToString();
        }
    }
}