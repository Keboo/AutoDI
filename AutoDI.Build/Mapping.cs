using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace AutoDI.Build
{
    internal class Mapping : IEnumerable<Registration>
    {
        private readonly ILogger _logger;

        private readonly List<Registration> _internalMaps = new List<Registration>();

        public static Mapping GetMapping(Settings settings, ICollection<TypeDefinition> allTypes, ILogger logger)
        {
            var rv = new Mapping(logger);

            //Order matters, when conflicts occur the last one wins.
            if (settings.Behavior.HasFlag(Behaviors.IncludeBaseClasses))
            {
                rv.AddBaseClasses(allTypes);
            }
            if (settings.Behavior.HasFlag(Behaviors.IncludeClasses))
            {
                rv.AddClasses(allTypes);
            }
            if (settings.Behavior.HasFlag(Behaviors.SingleInterfaceImplementation))
            {
                rv.AddSingleInterfaceImplementation(allTypes);
            }

            rv.AddSettingsMap(settings, allTypes);

            return rv;
        }

        private Mapping(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Add(TypeDefinition key, TypeDefinition targetType, Lifetime? lifetime, object source)
        {
            //TODO Better filtering, mostly just to remove <Module>
            if (targetType.Name.StartsWith("<") || targetType.Name.EndsWith(">")) return;

            //Issue 59 - don't allow compile-time mapping to open generics
            if (targetType.HasGenericParameters || key.HasGenericParameters)
            {
                _logger.Debug($"Ignoring map from '{key.FullName}' => '{targetType.FullName}' because it contains an open generic", DebugLogLevel.Verbose);
                return;
            }

            _logger.Debug($"{key.FullName} => {targetType.FullName} ({lifetime}) [{source}]", DebugLogLevel.Default);

            _internalMaps.Add(new Registration(key, targetType, lifetime ?? Settings.DefaultLifetime));
        }

        public void UpdateCreation(ICollection<MatchType> matchTypes)
        {
            for(int i = _internalMaps.Count - 1; i >= 0; i --)
            {
                foreach (MatchType matchType in matchTypes)
                {
                    Registration map = _internalMaps[i];
                    if (matchType.Matches(map.TargetType.FullName))
                    {
                        switch (matchType.Lifetime)
                        {
                            case Lifetime.None:
                                _internalMaps.RemoveAt(i);
                                break;
                            default:
                                map.Lifetime = matchType.Lifetime;
                                break;
                        }
                    }
                }
            }
        }

        public IEnumerator<Registration> GetEnumerator()
        {
            return _internalMaps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (Registration map in this)
            {
                sb.AppendLine(map.ToString());
            }
            return sb.ToString();
        }

        private void AddSingleInterfaceImplementation(IEnumerable<TypeDefinition> allTypes)
        {
            var types = new Dictionary<TypeReference, List<TypeDefinition>>(TypeComparer.FullName);

            foreach (TypeDefinition type in allTypes.Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (var @interface in type.Interfaces)
                {
                    if (!types.TryGetValue(@interface.InterfaceType, out List<TypeDefinition> list))
                    {
                        types.Add(@interface.InterfaceType, list = new List<TypeDefinition>());
                    }
                    list.Add(type);
                    //TODO: Base types?
                }
            }

            foreach (KeyValuePair<TypeReference, List<TypeDefinition>> kvp in types)
            {
                if (kvp.Value.Count != 1) continue;
                Add(kvp.Key.Resolve(), kvp.Value[0], 
                    GetLifetime(kvp.Value[0], Lifetime.LazySingleton), 
                    Behaviors.SingleInterfaceImplementation);
            }
        }

        private void AddClasses(IEnumerable<TypeDefinition> types)
        {
            foreach (TypeDefinition type in types.Where(t => t.IsClass && !t.IsAbstract))
            {
                Add(type, type, GetLifetime(type, Lifetime.Transient), Behaviors.IncludeClasses);
            }
        }

        private void AddBaseClasses(IEnumerable<TypeDefinition> types)
        {
            TypeDefinition GetBaseType(TypeDefinition type)
            {
                return type.BaseType?.Resolve();
            }

            foreach (TypeDefinition type in types.Where(t => t.IsClass && !t.IsAbstract && t.BaseType != null))
            {
                for (TypeDefinition t = GetBaseType(type); t != null; t = GetBaseType(t))
                {
                    if (t.FullName != typeof(object).FullName)
                    {
                        Add(t, type, GetLifetime(type, Lifetime.Transient), Behaviors.IncludeBaseClasses);
                    }
                }
            }
        }

        private void AddSettingsMap(Settings settings, IEnumerable<TypeDefinition> types)
        {
            Dictionary<string, TypeDefinition> allTypes = types.ToDictionary(x => x.FullName);

            foreach (string typeName in allTypes.Keys)
            {
                foreach (Map settingsMap in settings.Maps)
                {
                    if (settingsMap.TryGetMap(allTypes[typeName], out string mappedType))
                    {
                        if (allTypes.TryGetValue(mappedType, out TypeDefinition mapped))
                        {
                            if (settingsMap.Force || CanBeCastToType(allTypes[typeName], mapped))
                            {
                                Lifetime? lifetime = settingsMap.Lifetime;
                                if (lifetime is null)
                                {
                                    lifetime = GetLifetime(mapped, null);
                                }
                                Add(allTypes[typeName], mapped, lifetime, "Explicit Map");
                            }
                            else
                            {
                                _logger.Debug($"Found map '{typeName}' => '{mappedType}', but {mappedType} cannot be cast to '{typeName}'. Ignoring.", DebugLogLevel.Verbose);
                            }
                        }
                        else
                        {
                            _logger.Debug($"Found map '{typeName}' => '{mappedType}', but {mappedType} does not exist. Ignoring.", DebugLogLevel.Verbose);
                        }
                    }
                }
            }
            UpdateCreation(settings.Types);
        }

        private static Lifetime? GetLifetime(TypeDefinition targetType, Lifetime? @default)
        {
            CustomAttribute mapAttribute =
                targetType.CustomAttributes.FirstOrDefault(attribute => attribute.AttributeType.IsType<MapAttribute>());
            if (mapAttribute != null)
            {
                return (Lifetime)mapAttribute.ConstructorArguments.Single(x => x.Type.IsType<Lifetime>()).Value;
            }
            return @default;
        }

        private bool CanBeCastToType(TypeDefinition key, TypeDefinition targetType)
        {
            var comparer = TypeComparer.FullName;

            for (TypeDefinition t = targetType; t != null; t = t.BaseType?.Resolve())
            {
                if (comparer.Equals(key, t)) return true;
                if (t.Interfaces.Any(i => comparer.Equals(i.InterfaceType, key)))
                {
                    return true;
                }
            }
            _logger.Debug($"'{targetType.FullName}' cannot be cast to '{key.FullName}', ignoring", DebugLogLevel.Verbose);
            return false;
        }
    }
}