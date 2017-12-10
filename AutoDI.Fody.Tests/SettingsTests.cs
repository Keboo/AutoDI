using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using Mono.Cecil;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void CanLoadBasicSettings()
        {
            var xml = XElement.Parse(@"<AutoDI />");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.AreEqual(Behaviors.Default, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadIndividualBehavior()
        {
            var xml = XElement.Parse($@"<AutoDI behavior=""{Behaviors.SingleInterfaceImplementation}""/>");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.AreEqual(Behaviors.SingleInterfaceImplementation, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadCompositeBehavior()
        {
            var xml = XElement.Parse($@"<AutoDI behavior=""{Behaviors.SingleInterfaceImplementation},{Behaviors.IncludeClasses}""/>");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.AreEqual(Behaviors.SingleInterfaceImplementation | Behaviors.IncludeClasses, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadSettingsWithDeclaredType()
        {
            var xml = XElement.Parse(@"
                    <AutoDI>
                        <type name=""NS.MyType*"" lifetime=""Transient"" />
                    </AutoDI>");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.AreEqual(1, settings.Types.Count);
            Assert.IsTrue(settings.Types[0].Matches("NS.MyType2"));
            Assert.AreEqual(Lifetime.Transient, settings.Types[0].Lifetime);
        }

        [TestMethod]
        public void CanLoadSettingsWithSimpleMap()
        {
            var xml = XElement.Parse(@"
                    <AutoDI>
                        <map from=""IService"" to=""Service"" />
                    </AutoDI>");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.AreEqual(1, settings.Maps.Count);
            var type = new TypeDefinition("", "IService", TypeAttributes.Interface);
            Assert.IsTrue(settings.Maps[0].TryGetMap(type, out string mappedType));
            Assert.AreEqual("Service", mappedType);
        }

        [TestMethod]
        public void CanLoadSettingsWithRegexMap()
        {
            var xml = XElement.Parse(@"
                    <AutoDI>
                        <map from=""regex:ViewModels.(.*)"" to=""Views.$1"" />
                    </AutoDI>");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.AreEqual(1, settings.Maps.Count);
            var type = new TypeDefinition("ViewModels", "Test", TypeAttributes.Class);
            Assert.IsTrue(settings.Maps[0].TryGetMap(type, out var mappedType));
            Assert.AreEqual("Views.Test", mappedType);
        }

        [TestMethod]
        public void CanLoadSettingsWithIncludedAssembly()
        {
            var xml = XElement.Parse(@"
                    <AutoDI>
                        <assembly name=""MyAssembly.*"" />
                    </AutoDI>");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.AreEqual(1, settings.Assemblies.Count);
            var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("MyAssembly.Test", new Version(1, 0, 0)), "<Module>", ModuleKind.Dll);
            Assert.IsTrue(settings.Assemblies[0].Matches(assembly));
        }

        [TestMethod]
        public void CanSkipContainerGeneration()
        {
            var xml = XElement.Parse(@"<AutoDI GenerateRegistrations=""False"" />");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.IsFalse(settings.GenerateRegistrations);
        }

        [TestMethod]
        public void ErrorWhenRootElementContainsInvalidAttribute()
        {
            var xml = XElement.Parse(@"<AutoDI InvalidAttribute=""True"" />");

            TextExpectedParseException(xml, "'InvalidAttribute' is not a valid attribute for AutoDI");
        }

        [TestMethod]
        public void ErrorWhenBehaviorIsEmpty()
        {
            var xml = XElement.Parse(@"<AutoDI Behavior="""" />");

            TextExpectedParseException(xml, "'' is not a valid value for 'Behavior'");
        }

        [TestMethod]
        public void ErrorWhenBehaviorContainsAnInvalidValue()
        {
            var xml = XElement.Parse(@"<AutoDI Behavior=""InvalidValue"" />");

            TextExpectedParseException(xml, "'InvalidValue' is not a valid value for 'Behavior'");
        }

        [TestMethod]
        public void ErrorWhenAutoInitContainsAnInvalidValue()
        {
            var xml = XElement.Parse(@"<AutoDI AutoInit=""Foo"" />");

            TextExpectedParseException(xml, "'Foo' is not a valid value for 'AutoInit'");
        }

        [TestMethod]
        public void ErrorWhenGenerateRegistrationsContainsAnInvalidValue()
        {
            var xml = XElement.Parse(@"<AutoDI GenerateRegistrations=""Foo"" />");

            TextExpectedParseException(xml, "'Foo' is not a valid value for 'GenerateRegistrations'");
        }

        [TestMethod]
        public void ErrorWhenDebugLogLevelContainsAnInvalidValue()
        {
            var xml = XElement.Parse(@"<AutoDI DebugLogLevel=""Foo"" />");

            TextExpectedParseException(xml, "'Foo' is not a valid value for 'DebugLogLevel'");
        }

        [TestMethod]
        public void ErrorWhenChildNodeIsUnknown()
        {
            var xml = XElement.Parse(@"<AutoDI><Unknown /></AutoDI>");

            TextExpectedParseException(xml, "'Unknown' is not a valid child node of AutoDI");
        }

        [TestMethod]
        public void ErrorWhenAssemblyNodeDoesNotHaveName()
        {
            var xml = XElement.Parse(@"<AutoDI><Assembly /></AutoDI>");

            TextExpectedParseException(xml, "'Assembly' requires a value for 'Name'");
        }

        [TestMethod]
        public void ErrorWhenAssemblyNodeContainsInvalidAttribute()
        {
            var xml = XElement.Parse(@"<AutoDI><Assembly InvalidAttribute=""True""/></AutoDI>");

            TextExpectedParseException(xml, "'InvalidAttribute' is not a valid attribute for Assembly");
        }

        [TestMethod]
        public void ErrorWhenTypeNodeDoesNotHaveName()
        {
            var xml = XElement.Parse(@"<AutoDI><Type Lifetime=""Singleton"" /></AutoDI>");

            TextExpectedParseException(xml, "'Type' requires a value for 'Name'");
        }

        [TestMethod]
        public void ErrorWhenTypeNodeDoesNotHaveLifetime()
        {
            var xml = XElement.Parse(@"<AutoDI><Type Name=""MyClass"" /></AutoDI>");

            TextExpectedParseException(xml, "'Type' requires a value for 'Lifetime'");
        }

        [TestMethod]
        public void ErrorWhenTypeNodeDoesNotHaveValidLifetime()
        {
            var xml = XElement.Parse(@"<AutoDI><Type Name=""MyClass"" Lifetime=""Foo"" /></AutoDI>");

            TextExpectedParseException(xml, "'Foo' is not a valid value for 'Lifetime'");
        }

        [TestMethod]
        public void ErrorWhenTypeNodeContainsInvalidAttribute()
        {
            var xml = XElement.Parse(@"<AutoDI><Type Name=""MyClass"" Lifetime=""Singleton"" InvalidAttribute=""True""/></AutoDI>");

            TextExpectedParseException(xml, "'InvalidAttribute' is not a valid attribute for Type");
        }

        [TestMethod]
        public void ErrorWhenMapNodeDoesNotHaveFrom()
        {
            var xml = XElement.Parse(@"<AutoDI><Map To=""MyClass"" /></AutoDI>");

            TextExpectedParseException(xml, "'Map' requires a value for 'From'");
        }

        [TestMethod]
        public void ErrorWhenMapNodeDoesNotHaveTo()
        {
            var xml = XElement.Parse(@"<AutoDI><Map From=""MyClass"" /></AutoDI>");

            TextExpectedParseException(xml, "'Map' requires a value for 'To'");
        }

        [TestMethod]
        public void ErrorWhenMapNodeContainsInvalidAttribute()
        {
            var xml = XElement.Parse(@"<AutoDI><Map From=""IClass"" To=""MyClass"" InvalidAttribute=""True""/></AutoDI>");

            TextExpectedParseException(xml, "'InvalidAttribute' is not a valid attribute for Map");
        }

        private static void TextExpectedParseException(XElement xml, string expectedMessage)
        {
            try
            {
                Settings.Parse(new Settings(), xml);
            }
            catch (SettingsParseException e) when (e.Message == expectedMessage)
            {
                return;
            }
            Assert.Fail("Failed to get expected exception");
        }
    }
}
