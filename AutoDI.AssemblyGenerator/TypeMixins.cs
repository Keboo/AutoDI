using System.Text.RegularExpressions;

namespace AutoDI.AssemblyGenerator;

public static class TypeMixins
{
    public static bool Is<TExpected>(this object @object, Type? containerType = null)
    {
        return Is(@object?.GetType(), typeof(TExpected), containerType);
    }

    public static bool Is<TExpected>(this Type type, Type? containerType = null)
    {
        return Is(type, typeof(TExpected), containerType);
    }

    private static bool Is(Type? typeA, Type? typeB, Type? containerType)
    {
        return string.Equals(GetTypeName(typeA, containerType), GetTypeName(typeB, containerType),
            StringComparison.Ordinal);
    }

    public static string? GetTypeName(Type? type, Type? containerType)
    {
        if (type is null) return null;
        string rv = type.FullName;

        if (containerType?.Namespace != null && rv != null)
        {
            Regex genericPattern = new($@"(?<=\[){Regex.Escape(containerType.Namespace)}\.([^,]*),[^\]]*(?=\])");
            rv = genericPattern.Replace(rv, "$1");
            Regex namespacePattern = new(Regex.Escape(containerType.Namespace) + @"\.");
            rv = namespacePattern.Replace(rv, "");
        }
        return rv;
    }
}