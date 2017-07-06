using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AutoDI.Container.Fody
{
    internal class Settings
    {
        internal Settings(Behaviors behavior, bool injectContainer)
        {
            Behavior = behavior;
            InjectContainer = injectContainer;
        }

        public Behaviors Behavior { get; }

        public bool InjectContainer { get; }

        public IList<MatchType> Types { get; } = new List<MatchType>();

        public IList<Map> Maps { get; } = new List<Map>();

        public IList<MatchAssembly> Assemblies { get; } = new List<MatchAssembly>();

        public static Settings Parse(XElement rootElement)
        {
            Behaviors behavior = Behaviors.Default;
            if (rootElement == null) return new Settings(behavior, true);
            

            string behaviorAttribute = rootElement.GetAttributeValue("Behavior");
            if (behaviorAttribute != null)
            {
                behavior = Behaviors.None;
                foreach (string value in behaviorAttribute.Split(','))
                {
                    if (Enum.TryParse(value, out Behaviors @enum))
                        behavior |= @enum;
                }
            }

            if (!bool.TryParse(rootElement.GetAttributeValue(nameof(InjectContainer)) ?? bool.TrueString,
                out bool injectContainer))
            {
                injectContainer = true;
            }

            var rv = new Settings(behavior, injectContainer);

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
                string createStr = typeNode.GetAttributeValue("Create");
                Create create;
                if (createStr == null || !Enum.TryParse(createStr, out create))
                {
                    create = Create.LazySingleton;
                }

                rv.Types.Add(new MatchType(typePattern, create));
            }

            foreach (XElement mapNode in rootElement.DescendantNodes().OfType<XElement>()
                .Where(x => string.Equals(x.Name.LocalName, "Map", StringComparison.OrdinalIgnoreCase)))
            {
                string from = mapNode.GetAttributeValue("From");
                if (string.IsNullOrWhiteSpace(from)) continue;
                string to = mapNode.GetAttributeValue("To");
                if (string.IsNullOrWhiteSpace(to)) continue;
                if (!bool.TryParse(mapNode.GetAttributeValue("force") ?? bool.FalseString, out bool force))
                {
                    force = false;
                }
                rv.Maps.Add(new Map(from, to, force));
            }

            return rv;
        }
    }
}