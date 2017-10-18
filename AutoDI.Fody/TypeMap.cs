using System.Collections.Generic;
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
                typeKey.Keys.Add(type);
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

        public bool RemoveKey(TypeDefinition key)
        {
            bool rv = false;
            foreach (KeyValuePair<Lifetime, TypeLifetime> kvp in _keys)
            {
                rv |= kvp.Value.Keys.Remove(key);
            }
            return rv;
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
        public ICollection<TypeDefinition> Keys { get; } = new HashSet<TypeDefinition>(TypeComparer.FullName);
        
        public Lifetime Lifetime { get; }

        public TypeLifetime(Lifetime lifetime, params TypeDefinition[] keys)
        {
            foreach (TypeDefinition key in keys)
            {
                Keys.Add(key);
            }
            Lifetime = lifetime;
        }
    }
}