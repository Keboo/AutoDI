using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AutoDI.Container.Fody
{
    internal class Settings
    {
        public Behaviors Behavior { get; set; } = Behaviors.Default;

        public bool InjectContainer { get; set; } = true;

        public DebugLogLevel DebugLogLevel { get; set; } = DebugLogLevel.Default;

        public IList<MatchType> Types { get; } = new List<MatchType>();

        public IList<Map> Maps { get; } = new List<Map>();

        public IList<MatchAssembly> Assemblies { get; } = new List<MatchAssembly>();

        public static Settings Parse(XElement rootElement)
        {
            var rv = new Settings();
            if (rootElement == null) return rv;

            string behaviorAttribute = rootElement.GetAttributeValue(nameof(Behavior));
            if (behaviorAttribute != null)
            {
                Behaviors behavior = Behaviors.None;
                foreach (string value in behaviorAttribute.Split(','))
                {
                    if (Enum.TryParse(value, out Behaviors @enum))
                        behavior |= @enum;
                }
                rv.Behavior = behavior;
            }

            if (bool.TryParse(rootElement.GetAttributeValue(nameof(InjectContainer)) ?? bool.TrueString,
                out bool injectContainer))
            {
                rv.InjectContainer = injectContainer;
            }

            if (Enum.TryParse(rootElement.GetAttributeValue(nameof(DebugLogLevel)) ?? nameof(DebugLogLevel.Default),
                out DebugLogLevel debugLogLevel))
            {
                rv.DebugLogLevel = debugLogLevel;
            }

            foreach (XElement assemblyNode in rootElement.DescendantNodes().OfType<XElement>()
                .Where(x => string.Equals(x.Name.LocalName, "Assembly", StringComparison.OrdinalIgnoreCase)))
            {
                string assemblyName = assemblyNode.GetAttributeValue("Name");
                if (string.IsNullOrWhiteSpace(assemblyName)) continue;

                rv.Assemblies.Add(new MatchAssembly(assemblyName));
            }

            foreach (XElement typeNode in rootElement.DescendantNodes().OfType<XElement>()
                .Where(x => string.Equals(x.Name.LocalName, "Type", StringComparison.OrdinalIgnoreCase)))
            {
                string typePattern = typeNode.GetAttributeValue("Name");
                if (string.IsNullOrWhiteSpace(typePattern)) continue;
                string createStr = typeNode.GetAttributeValue(nameof(Lifetime));
                Lifetime lifetime;
                if (createStr == null || !Enum.TryParse(createStr, out lifetime))
                {
                    lifetime = Lifetime.LazySingleton;
                }

                rv.Types.Add(new MatchType(typePattern, lifetime));
            }

            foreach (XElement mapNode in rootElement.DescendantNodes().OfType<XElement>()
                .Where(x => string.Equals(x.Name.LocalName, "Map", StringComparison.OrdinalIgnoreCase)))
            {
                string from = mapNode.GetAttributeValue("From");
                if (string.IsNullOrWhiteSpace(from)) continue;
                string to = mapNode.GetAttributeValue("To");
                if (string.IsNullOrWhiteSpace(to)) continue;
                if (!bool.TryParse(mapNode.GetAttributeValue("Force") ?? bool.FalseString, out bool force))
                {
                    force = false;
                }
                rv.Maps.Add(new Map(from, to, force));
            }

            return rv;
        }
    }
}