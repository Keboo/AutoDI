using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace AutoDI.Fody
{
    internal class TypeComparer
    {
        public static IEqualityComparer<TypeReference> FullName { get; } = new Comparer<string>(td => td.FullName);

        private class Comparer<T> : IEqualityComparer<TypeReference>
        {
            private readonly Func<TypeReference, T> _accessor;

            public Comparer(Func<TypeReference, T> accessor)
            {
                _accessor = accessor;
            }
            public bool Equals(TypeReference x, TypeReference y)
            {
                return EqualityComparer<T>.Default.Equals(_accessor(x), _accessor(y));
            }

            public int GetHashCode(TypeReference obj)
            {
                return EqualityComparer<T>.Default.GetHashCode(_accessor(obj));
            }
        }
    }
}