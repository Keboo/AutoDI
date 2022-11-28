using Mono.Cecil;

namespace AutoDI.Build;

public interface ITypeResolver
{
    TypeDefinition? ResolveType(string fullTypeName);
}