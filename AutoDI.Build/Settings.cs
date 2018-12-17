using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDI.Build
{
    internal class Settings
    {
        public static Settings Load(TypeResolver typeResolver)
        {
            var settings = new Settings();
            foreach (CustomAttribute attribute in typeResolver.GetAllModules()
                .SelectMany(m => m.Assembly.CustomAttributes))
            {
                if (attribute.AttributeType.IsType<SettingsAttribute>())
                {
                    foreach (CustomAttributeNamedArgument property in attribute.Properties)
                    {
                        if (property.Argument.Value != null)
                        {
                            switch (property.Name)
                            {
                                case nameof(SettingsAttribute.AutoInit):
                                    settings.AutoInit = (bool) property.Argument.Value;
                                    break;
                                case nameof(SettingsAttribute.Behavior):
                                    settings.Behavior = (Behaviors) property.Argument.Value;
                                    break;
                                case nameof(SettingsAttribute.DebugLogLevel):
                                    settings.DebugLogLevel = (DebugLogLevel) property.Argument.Value;
                                    break;
                                case nameof(SettingsAttribute.GenerateRegistrations):
                                    settings.GenerateRegistrations = (bool) property.Argument.Value;
                                    break;
                                case nameof(SettingsAttribute.DebugCodeGeneration):
                                    settings.DebugCodeGeneration = (CodeLanguage) property.Argument.Value;
                                    break;
                            }
                        }
                    }
                }
                else if (attribute.AttributeType.IsType<MapAttribute>())
                
{
                    Lifetime? lifetime = null;
                    string targetTypePattern = null;
                    string sourceTypePattern = null;
                    bool force = false;

                    var ctorParameterNames = GetConstructorParameterNames();
                    if (ctorParameterNames == null) throw new SettingsParseException($"Unknown {nameof(MapAttribute)} constructor");

                    for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
                    {
                        SetFromName(ctorParameterNames[i], attribute.ConstructorArguments[i]);
                    }

                    foreach (CustomAttributeNamedArgument property in attribute.Properties)
                    {
                        SetFromName(property.Name, property.Argument);
                    }

                    if (targetTypePattern != null && sourceTypePattern != null)
                    {
                        settings.Maps.Add(new Map(sourceTypePattern, targetTypePattern, force, lifetime));
                    }
                    else if (lifetime != null && targetTypePattern != null)
                    {
                        settings.Types.Add(new MatchType(targetTypePattern, lifetime.Value));
                    }
                    else
                    {
                        throw new SettingsParseException(
                            $"Assembly level {nameof(MapAttribute)} must contain both a {nameof(MapAttribute.Lifetime)} and a {nameof(MapAttribute.TargetTypePattern)} or a {nameof(MapAttribute.SourceTypePattern)} and a {nameof(MapAttribute.TargetTypePattern)}.");
                    }

                    void SetFromName(string name, CustomAttributeArgument argument)
                    {
                        switch (name)
                        {
                            case nameof(MapAttribute.Lifetime):
                                if (argument.Type.IsType<Lifetime>())
                                {
                                    lifetime = (Lifetime)argument.Value;
                                }
                                break;
                            case nameof(MapAttribute.SourceTypePattern):
                                if (argument.Type.IsType<string>())
                                {
                                    sourceTypePattern = (string) argument.Value;
                                }
                                else if (argument.Type.IsType<Type>())
                                {
                                    sourceTypePattern = ((TypeDefinition)argument.Value).FullName;
                                }
                                break;
                            case nameof(MapAttribute.TargetTypePattern):
                                if (argument.Type.IsType<string>())
                                {
                                    targetTypePattern = (string) argument.Value;
                                }
                                else if (argument.Type.IsType<Type>())
                                {
                                    targetTypePattern = ((TypeDefinition)argument.Value).FullName;
                                }
                                break;
                            case nameof(MapAttribute.Force):
                                if (argument.Type.IsType<bool>())
                                {
                                    force = (bool) argument.Value;
                                }
                                break;
                        }
                    }

                    IList<string> GetConstructorParameterNames()
                    {
                        switch (attribute.ConstructorArguments.Count)
                        {
                            case 1:
                                if (attribute.ConstructorArguments[0].Type.IsType<Lifetime>())
                                {
                                    return new[] { nameof(MapAttribute.Lifetime) };
                                }
                                break;
                            case 2:
                                if (attribute.ConstructorArguments[0].Type.IsType<Type>() &&
                                    attribute.ConstructorArguments[1].Type.IsType<Lifetime>())
                                {
                                    return new[]
                                    {
                                        nameof(MapAttribute.TargetTypePattern),
                                        nameof(MapAttribute.Lifetime)
                                    };
                                }
                                if (attribute.ConstructorArguments[0].Type.IsType<string>() &&
                                    attribute.ConstructorArguments[1].Type.IsType<Lifetime>())
                                {
                                    return new[]
                                    {
                                        nameof(MapAttribute.TargetTypePattern),
                                        nameof(MapAttribute.Lifetime)
                                    };
                                }
                                if (attribute.ConstructorArguments[0].Type.IsType<string>() &&
                                    attribute.ConstructorArguments[1].Type.IsType<string>())
                                {
                                    return new[]
                                    {
                                        nameof(MapAttribute.SourceTypePattern),
                                        nameof(MapAttribute.TargetTypePattern)
                                    };
                                }
                                if (attribute.ConstructorArguments[0].Type.IsType<Type>() &&
                                    attribute.ConstructorArguments[1].Type.IsType<Type>())
                                {
                                    return new[]
                                    {
                                        nameof(MapAttribute.SourceTypePattern),
                                        nameof(MapAttribute.TargetTypePattern)
                                    };
                                }
                                break;
                            case 3:
                                if (attribute.ConstructorArguments[0].Type.IsType<string>() &&
                                    attribute.ConstructorArguments[1].Type.IsType<string>() &&
                                    attribute.ConstructorArguments[2].Type.IsType<Lifetime>())
                                {
                                    return new[]
                                    {
                                        nameof(MapAttribute.SourceTypePattern),
                                        nameof(MapAttribute.TargetTypePattern),
                                        nameof(MapAttribute.Lifetime)
                                    };
                                }
                                if (attribute.ConstructorArguments[0].Type.IsType<Type>() &&
                                    attribute.ConstructorArguments[1].Type.IsType<Type>() &&
                                    attribute.ConstructorArguments[2].Type.IsType<Lifetime>())
                                {
                                    return new[]
                                    {
                                        nameof(MapAttribute.SourceTypePattern),
                                        nameof(MapAttribute.TargetTypePattern),
                                        nameof(MapAttribute.Lifetime)
                                    };
                                }
                                break;
                        }
                        return null;
                    }
                }
                else if (attribute.AttributeType.IsType<IncludeAssemblyAttribute>())
                {
                    string assemblyPattern = attribute.ConstructorArguments.FirstOrDefault().Value as string;

                    if (string.IsNullOrWhiteSpace(assemblyPattern))
                    {
                        throw new SettingsParseException($"{nameof(IncludeAssemblyAttribute)} must specify an assembly to include");
                    }
                    settings.Assemblies.Add(new MatchAssembly(assemblyPattern));
                }
            }

            return settings;
        }

        internal const Lifetime DefaultLifetime = Lifetime.LazySingleton;

        public Behaviors Behavior { get; set; } = Behaviors.Default;

        /// <summary>
        /// Automatically initialize AutoDI in assembly entry point (if available)
        /// </summary>
        public bool AutoInit { get; set; } = true;

        /// <summary>
        /// Generate registration calls no the container. Setting to false will negate AutoInit.
        /// </summary>
        public bool GenerateRegistrations { get; set; } = true;

        public bool DebugExceptions { get; set; }

        public DebugLogLevel DebugLogLevel { get; set; } = DebugLogLevel.Default;

        public CodeLanguage DebugCodeGeneration { get; set; } = CodeLanguage.None;

        public IList<MatchType> Types { get; } = new List<MatchType>();

        public IList<Map> Maps { get; } = new List<Map>();

        public IList<MatchAssembly> Assemblies { get; } = new List<MatchAssembly>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("AutoDI Settings:");
            sb.AppendLine($"  Behavior(s): {Behavior}");
            sb.AppendLine($"  AutoInit: {AutoInit}");
            sb.AppendLine($"  GenerateRegistrations: {GenerateRegistrations}");
            sb.AppendLine($"  DebugLogLevel: {DebugLogLevel}");
            sb.AppendLine($"  DebugExceptions: {DebugExceptions}");
            sb.Append("  Included Assemblies: ");
            if (Assemblies.Any())
            {
                sb.AppendLine();
                foreach (MatchAssembly assembly in Assemblies)
                {
                    sb.AppendLine($"    {assembly}");
                }
            }
            else
            {
                sb.AppendLine("<none>");
            }

            sb.Append("  Maps: ");
            if (Maps.Any())
            {
                sb.AppendLine();
                foreach (Map map in Maps)
                {
                    sb.AppendLine($"    {map}");
                }
            }
            else
            {
                sb.AppendLine("<none>");
            }

            sb.Append("  Type Lifetimes: ");
            if (Types.Any())
            {
                sb.AppendLine();
                foreach (MatchType type in Types)
                {
                    sb.AppendLine($"    {type}");
                }
            }
            else
            {
                sb.AppendLine("<none>");
            }


            return sb.ToString();
        }

        //public static Settings Parse(Settings settings, XElement rootElement)
        //{
        //    if (rootElement == null) return settings;
        //
        //    ParseAttributes(rootElement, 
        //        Attrib.OptionalBool(nameof(AutoInit), x => settings.AutoInit = x),
        //        Attrib.OptionalBool(nameof(GenerateRegistrations), x => settings.GenerateRegistrations = x),
        //        Attrib.OptionalEnum<DebugLogLevel>(nameof(DebugLogLevel), x => settings.DebugLogLevel = x),
        //        Attrib.OptionalBool(nameof(DebugExceptions), x => settings.DebugExceptions = x),
        //        Attrib.Create(nameof(Behavior), x => settings.Behavior = x, (string x, out Behaviors behavior) =>
        //        {
        //            behavior = Behaviors.None;
        //
        //            if (string.IsNullOrWhiteSpace(x))
        //                return false;
        //
        //            foreach (string value in x.Split(','))
        //            {
        //                if (Enum.TryParse(value, out Behaviors @enum))
        //                    behavior |= @enum;
        //                else
        //                    return false;
        //            }
        //            return true;
        //        }, false),
        //        Attrib.OptionalEnum<CodeLanguage>(nameof(DebugCodeGeneration), x => settings.DebugCodeGeneration = x));
        //
        //    foreach (XElement element in rootElement.DescendantNodes().OfType<XElement>())
        //    {
        //        if (element.Name.LocalName.Equals("Assembly", StringComparison.OrdinalIgnoreCase))
        //        {
        //            string assemblyName = "";
        //            ParseAttributes(element, Attrib.RequiredString("Name", x => assemblyName = x));
        //            settings.Assemblies.Add(new MatchAssembly(assemblyName));
        //        }
        //        else if (element.Name.LocalName.Equals("Type", StringComparison.OrdinalIgnoreCase))
        //        {
        //            string typePattern = "";
        //            Lifetime lifetime = DefaultLifetime;
        //            ParseAttributes(element, Attrib.RequiredString("Name", x => typePattern = x),
        //                Attrib.RequiredEnum<Lifetime>("Lifetime", x => lifetime = x));
        //            settings.Types.Add(new MatchType(typePattern, lifetime));
        //        }
        //        else if (element.Name.LocalName.Equals("Map", StringComparison.OrdinalIgnoreCase))
        //        {
        //            string from = "";//GetRequiredString(element, "From");
        //            string to = "";//GetRequiredString(element, "To");
        //            bool force = false;
        //            Lifetime? lifetime = null;
        //            ParseAttributes(element, Attrib.RequiredString("From", x => from = x),
        //                Attrib.RequiredString("To", x => to = x),
        //                Attrib.OptionalBool("Force", x => force = x),
        //                Attrib.OptionalEnum<Lifetime>("Lifetime", x => lifetime = x));
        //
        //            settings.Maps.Add(new Map(from, to, force, lifetime));
        //        }
        //        else
        //        {
        //            throw new SettingsParseException($"'{element.Name.LocalName}' is not a valid child node of AutoDI");
        //        }
        //    }
        //
        //    return settings;
        //
        //    void ParseAttributes(XElement element, params IAttribute[] attributes)
        //    {
        //        Dictionary<string, IAttribute> attributesByName =
        //            attributes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        //
        //        foreach (XAttribute attribute in element.Attributes())
        //        {
        //            if (attributesByName.TryGetValue(attribute.Name.LocalName, out IAttribute attrib))
        //            {
        //                attrib.Set(attribute.Value);
        //                attributesByName.Remove(attribute.Name.LocalName);
        //            }
        //            else
        //            {
        //                throw new SettingsParseException($"'{attribute.Name.LocalName}' is not a valid attribute for {element.Name.LocalName}");
        //            }
        //        }
        //
        //        foreach (IAttribute attribute in attributesByName.Values.Where(x => x.IsRequired))
        //        {
        //            throw new SettingsParseException($"'{element.Name.LocalName}' requires a value for '{attribute.Name}'");
        //        }
        //    }
        //}
    }
}