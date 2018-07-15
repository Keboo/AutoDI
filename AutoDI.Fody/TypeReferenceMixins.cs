using System;
using System.Linq;
using Mono.Cecil;

namespace AutoDI.Fody
{
    internal static class TypeReferenceMixins
    {
        internal static bool IsType<T>(this TypeReference reference)
        {
            return IsType(reference, typeof(T));
        }

        internal static bool IsType(this TypeReference reference, Type type)
        {
            return string.Equals(reference.FullName, type.FullName, StringComparison.Ordinal);
        }

        internal static bool IsType(this TypeReference reference, TypeReference type)
        {
            return string.Equals(reference.FullName, type.FullName, StringComparison.Ordinal);
        }

        internal static string FullNameCSharp(this TypeReference type)
        {
            return type.FullName.Replace('/', '.');
        }

        internal static string DeclarationCSharp(this TypeDefinition type)
        {
            return $"{type.Attributes.ProtectionModifierCSharp()} class {type.NameCSharp(true)}";
        }

        internal static string NameCSharp(this TypeReference type, bool includeGenericParameters = false)
        {
            string rv = type.Name;
            int index = rv.IndexOf('`');
            if (index >= 0)
            {
                rv = rv.Substring(0, index);
            }

            if (includeGenericParameters && type.HasGenericParameters)
            {
                rv += $"<{string.Join(", ", type.GenericParameters.Select(x => x.Name))}>";
            }
            return rv;
        }

        internal static string ProtectionModifierCSharp(this MethodAttributes methodAttributes)
        {
            if (methodAttributes.HasFlag(MethodAttributes.Public))
            {
                return "public";
            }
            if (methodAttributes.HasFlag(MethodAttributes.Assembly))
            {
                return "internal";
            }
            if (methodAttributes.HasFlag(MethodAttributes.FamORAssem))
            {
                return "protected internal";
            }
            if (methodAttributes.HasFlag(MethodAttributes.Family))
            {
                return "protected";
            }
            if (methodAttributes.HasFlag(MethodAttributes.FamANDAssem))
            {
                return "private protected";
            }
            if (methodAttributes.HasFlag(MethodAttributes.Private))
            {
                return "private";
            }
            return "";
        }

        internal static string ProtectionModifierCSharp(this TypeAttributes typeAttributes)
        {
            if (typeAttributes.HasFlag(TypeAttributes.NestedFamORAssem)) //Must come before NestedFamANDAssem
            {
                return "protected internal";
            }
            if (typeAttributes.HasFlag(TypeAttributes.NestedFamANDAssem))
            {
                return "private protected";
            }
            if (typeAttributes.HasFlag(TypeAttributes.NestedFamily) && !typeAttributes.HasFlag(TypeAttributes.Public))
            {
                return "protected";
            }
            
            if (typeAttributes.HasFlag(TypeAttributes.NestedAssembly))
            {
                return "internal";
            }
            if (typeAttributes.HasFlag(TypeAttributes.Public) ^ 
                typeAttributes.HasFlag(TypeAttributes.NestedPublic))
            {
                return "public";
            }
            if (typeAttributes.HasFlag(TypeAttributes.NestedPrivate)) //Must come after public
            {
                return "private";
            }
            return "internal";
        }
    }
}