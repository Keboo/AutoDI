using System;
using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDI.Fody
{
    internal class Mapping : IEnumerable<TypeMap>
    {
        private readonly Action<string, DebugLogLevel> _logMessage;
        private readonly Dictionary<string, TypeMap> _maps = new Dictionary<string, TypeMap>();

        public Mapping(Action<string, DebugLogLevel> logMessage)
        {
            _logMessage = logMessage ?? throw new ArgumentNullException(nameof(logMessage));
        }

        public void Add(TypeDefinition key, TypeDefinition targetType, DuplicateKeyBehavior behavior, Lifetime? lifetime)
        {
            //TODO Better filtering, mostly just to remove <Module>
            if (targetType.Name.StartsWith("<") || targetType.Name.EndsWith(">")) return;

            //Issue 59 - don't allow compile-time mapping to open generics
            if (targetType.HasGenericParameters || key.HasGenericParameters)
            {
                _logMessage($"Ignoring map from '{key.FullName}' => '{targetType.FullName}' because it contains an open generic", DebugLogLevel.Verbose);
                return;
            }

            //Last key in wins, this allows for manual mapping to override things added with behaviors
            bool duplicateKey = false;
            foreach (var kvp in _maps.Where(kvp => kvp.Value.RemoveKey(key)))
            {
                duplicateKey = true;
            }

            if (duplicateKey && behavior == DuplicateKeyBehavior.RemoveAll)
            {
                _logMessage($"Removing duplicate maps with service key '{key.FullName}'", DebugLogLevel.Verbose);
                return;
            }

            if (!_maps.TryGetValue(targetType.FullName, out TypeMap typeMap))
            {
                _maps[targetType.FullName] = typeMap = new TypeMap(targetType);
            }
            
            typeMap.AddKey(key, lifetime ?? Settings.DefaultLifetime);
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
                                _maps[targetType].SetLifetime(type.Lifetime);
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