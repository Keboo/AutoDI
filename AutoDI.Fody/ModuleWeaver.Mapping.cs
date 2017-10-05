// ReSharper disable once CheckNamespace

using AutoDI.Fody;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using AutoDI;
using Map = AutoDI.Fody.Map;

// ReSharper disable once CheckNamespace
partial class ModuleWeaver
{
    private Mapping GetMapping(Settings settings, ICollection<TypeDefinition> allTypes)
    {
        var rv = new Mapping();

        if (settings.Behavior.HasFlag(Behaviors.SingleInterfaceImplementation))
        {
            AddSingleInterfaceImplementation(rv, allTypes);
        }
        if (settings.Behavior.HasFlag(Behaviors.IncludeClasses))
        {
            AddClasses(rv, allTypes);
        }
        if (settings.Behavior.HasFlag(Behaviors.IncludeBaseClasses))
        {
            AddBaseClasses(rv, allTypes);
        }

        AddSettingsMap(settings, rv, allTypes);

        return rv;
    }

    private static void AddSingleInterfaceImplementation(Mapping map, IEnumerable<TypeDefinition> allTypes)
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
                //TODO: Base types
            }
        }

        foreach (KeyValuePair<TypeReference, List<TypeDefinition>> kvp in types)
        {
            if (kvp.Value.Count != 1) continue;
            map.Add(kvp.Key.Resolve(), kvp.Value[0], DuplicateKeyBehavior.RemoveAll);
        }
    }

    private static void AddClasses(Mapping map, IEnumerable<TypeDefinition> types)
    {
        foreach (TypeDefinition type in types.Where(t => t.IsClass && !t.IsAbstract))
        {
            map.Add(type, type, DuplicateKeyBehavior.RemoveAll);
        }
    }

    private static void AddBaseClasses(Mapping map, IEnumerable<TypeDefinition> types)
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
                    map.Add(t, type, DuplicateKeyBehavior.RemoveAll);
                }
            }
        }
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
        InternalLogDebug($"'{targetType.FullName}' cannot be cast to '{key.FullName}', ignoring", DebugLogLevel.Verbose);
        return false;
    }

    private void AddSettingsMap(Settings settings, Mapping map, IEnumerable<TypeDefinition> types)
    {
        var allTypes = types.ToDictionary(x => x.FullName);

        foreach (string typeName in allTypes.Keys)
        {
            foreach (Map settingsMap in settings.Maps)
            {
                if (settingsMap.TryGetMap(typeName, out string mappedType))
                {
                    if (allTypes.TryGetValue(mappedType, out TypeDefinition mapped))
                    {
                        if (settingsMap.Force || CanBeCastToType(allTypes[typeName], mapped))
                        {
                            map.Add(allTypes[typeName], mapped, DuplicateKeyBehavior.Replace);
                        }
                        else
                        {
                            InternalLogDebug($"Found map '{typeName}' => '{mappedType}', but {mappedType} cannot be cast to '{typeName}'. Ignoring.", DebugLogLevel.Verbose);
                        }
                    }
                    else
                    {
                        InternalLogDebug($"Found map '{typeName}' => '{mappedType}', but {mappedType} does not exist. Ignoring.", DebugLogLevel.Verbose);
                    }
                }
            }
        }
        map.UpdateCreation(settings.Types);
    }

}