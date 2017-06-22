using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AutoDI.Container.Fody
{
    internal class Settings
    {
        internal Settings(Behaviors behavior)
        {
            Behavior = behavior;
        }

        public Behaviors Behavior { get; }

        public IList<MatchType> Types { get; } = new List<MatchType>();

        public IList<Map> Maps { get; } = new List<Map>();

        public static Settings Parse(XElement fodyWeaversRoot)
        {
            Behaviors behavior = Behaviors.Default;
            if (fodyWeaversRoot == null) return new Settings(behavior);
            XElement containerRoot = fodyWeaversRoot.DescendantNodes().OfType<XElement>().FirstOrDefault(x => x.Name.LocalName == "AutoDI.Container");
            if (containerRoot == null)
                throw new InvalidOperationException("Could not find AutoDI.Container element in FodyWeavers.xml"); //How did we get here?

            var behaviorAttribute = containerRoot.GetAttributeValue("Behavior");
            if (behaviorAttribute != null)
            {
                behavior = Behaviors.None;
                foreach (string value in behaviorAttribute.Split(','))
                {
                    if (Enum.TryParse(value, out Behaviors @enum))
                        behavior |= @enum;
                }
            }

            var rv = new Settings(behavior);

            foreach (XElement typeNode in containerRoot.DescendantNodes().OfType<XElement>()
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

            foreach (XElement mapNode in containerRoot.DescendantNodes().OfType<XElement>()
                .Where(x => string.Equals(x.Name.LocalName, "Map", StringComparison.OrdinalIgnoreCase)))
            {
                string from = mapNode.GetAttributeValue("From");
                if (string.IsNullOrWhiteSpace(from)) continue;
                string to = mapNode.GetAttributeValue("To");
                if (string.IsNullOrWhiteSpace(to)) continue;

                rv.Maps.Add(new Map(from, to));
            }

            return rv;
        }
    }
}