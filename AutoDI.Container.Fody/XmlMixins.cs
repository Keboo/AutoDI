﻿using System;
using System.Linq;
using System.Xml.Linq;

namespace AutoDI.Container.Fody
{
    internal static class XmlMixins
    {
        public static string GetAttributeValue(this XElement element, string attributeName)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return element.Attributes()
                .FirstOrDefault(a => string.Equals(a.Name.LocalName, attributeName, StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }
    }
}