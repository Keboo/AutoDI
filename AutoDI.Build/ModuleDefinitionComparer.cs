using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace AutoDI.Build
{
    internal class ModuleDefinitionComparer
    {
        public static IEqualityComparer<ModuleDefinition> FileName { get; } = new Comparer<string>(md => md.FileName);

        private class Comparer<T> : IEqualityComparer<ModuleDefinition>
        {
            private readonly Func<ModuleDefinition, T> _accessor;

            public Comparer(Func<ModuleDefinition, T> accessor)
            {
                _accessor = accessor;
            }
            public bool Equals(ModuleDefinition x, ModuleDefinition y)
            {
                return EqualityComparer<T>.Default.Equals(_accessor(x), _accessor(y));
            }

            public int GetHashCode(ModuleDefinition obj)
            {
                return EqualityComparer<T>.Default.GetHashCode(_accessor(obj));
            }
        }
    }
}