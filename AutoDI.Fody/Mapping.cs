using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDI.Fody
{
    internal class Mapping : IEnumerable<TypeMap>
    {
        private readonly Dictionary<string, TypeMap> _maps = new Dictionary<string, TypeMap>();

        public void Add(TypeDefinition key, TypeDefinition targetType, DuplicateKeyBehavior behavior)
        {
            //TODO Better filtering
            if (targetType.FullName.Contains('<') || targetType.FullName.Contains('>')) return;


            //Last key in wins, this allows for manual mapping to override things added with behaviors
            bool duplicateKey = false;
            foreach (var kvp in _maps.Where(kvp => kvp.Value.Keys.Contains(key)).ToList())
            {
                duplicateKey = true;
                kvp.Value.Keys.Remove(key);
                if (!kvp.Value.Keys.Any())
                {
                    _maps.Remove(kvp.Key);
                }
            }

            if (duplicateKey && behavior == DuplicateKeyBehavior.RemoveAll)
            {
                return;
            }

            if (!_maps.TryGetValue(targetType.FullName, out TypeMap typeMap))
            {
                _maps[targetType.FullName] = typeMap = new TypeMap(targetType);
            }

            typeMap.Keys.Add(key);
        }

        public void UpdateCreation(ICollection<MatchType> matchTypes)
        {
            foreach (string targetType in _maps.Keys.ToList())
            {
                foreach (MatchType type in matchTypes)
                {
                    if (type.Matches(targetType))
                    {
                        switch (type.Lifetime)
                        {
                            case Lifetime.None:
                                _maps.Remove(targetType);
                                break;
                            default:
                                _maps[targetType].Lifetime = type.Lifetime;
                                break;
                        }
                    }
                }
            }
        }

        public IEnumerator<TypeMap> GetEnumerator()
        {
            return _maps.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (TypeMap map in this)
            {
                sb.AppendLine(map.ToString());
            }
            return sb.ToString();
        }
    }
}