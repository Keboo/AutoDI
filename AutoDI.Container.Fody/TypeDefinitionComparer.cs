using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace AutoDI.Container.Fody
{
    internal class TypeDefinitionComparer
    {
        public static IEqualityComparer<TypeDefinition> FullName { get; } = new Comparer<string>(td => td.FullName);

        private class Comparer<T> : IEqualityComparer<TypeDefinition>
        {
            private readonly Func<TypeDefinition, T> _accessor;

            public Comparer(Func<TypeDefinition, T> accessor)
            {
                _accessor = accessor;
            }
            public bool Equals(TypeDefinition x, TypeDefinition y)
            {
                return EqualityComparer<T>.Default.Equals(_accessor(x), _accessor(y));
            }

            public int GetHashCode(TypeDefinition obj)
            {
                return EqualityComparer<T>.Default.GetHashCode(_accessor(obj));
            }
        }
    }
}