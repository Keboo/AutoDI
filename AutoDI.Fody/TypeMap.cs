using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace AutoDI.Fody
{
    internal class TypeMap
    {
        private readonly Dictionary<Lifetime, TypeLifetime> _keys = new Dictionary<Lifetime, TypeLifetime>();

        public TypeMap(TypeDefinition targetType)
        {
            TargetType = targetType;
        }

        public TypeDefinition TargetType { get; }

        public ICollection<TypeLifetime> Lifetimes => _keys.Values;

        public void AddKey(TypeDefinition type, Lifetime lifetime)
        {
            if (_keys.TryGetValue(lifetime, out TypeLifetime typeKey))
            {
                typeKey.Add(type);
            }
            else
            {
                _keys[lifetime] = new TypeLifetime(lifetime, type);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (TypeLifetime key in Lifetimes)
            {
                foreach (TypeDefinition typeKey in key.Keys)
                {
                    sb.AppendLine($"{typeKey.FullName} => {TargetType.FullName} ({key.Lifetime})");
                }
            }
            return sb.ToString();
        }

        public void SetLifetime(Lifetime lifetime)
        {
            var typeKey = new TypeLifetime(lifetime, _keys.Values.SelectMany(tk => tk.Keys).ToArray());
            _keys.Clear();
            _keys[lifetime] = typeKey;
        }
    }

    internal class TypeLifetime
    {
        private readonly List<TypeDefinition> _keys = new List<TypeDefinition>();
        public IReadOnlyCollection<TypeDefinition> Keys { get; }
        
        public Lifetime Lifetime { get; }

        public TypeLifetime(Lifetime lifetime, params TypeDefinition[] keys)
        {
            Keys = new ReadOnlyCollection<TypeDefinition>(_keys);
            foreach (TypeDefinition key in keys)
            {
                Add(key);
            }
            Lifetime = lifetime;
        }

        public void Add(TypeDefinition key)
        {
            if (!_keys.Contains(key, TypeComparer.FullName))
            {
                _keys.Add(key);
            }
        }
    }
}