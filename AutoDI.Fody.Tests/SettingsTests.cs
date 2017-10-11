using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

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
            string mappedType;
            Assert.IsTrue(settings.Maps[0].TryGetMap("IService", out mappedType));
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
            Assert.IsTrue(settings.Maps[0].TryGetMap("ViewModels.Test", out var mappedType));
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
            Assert.IsTrue(settings.Assemblies[0].Matches("MyAssembly.Test"));
        }

        [TestMethod]
        public void CanSkipContainerGeneration()
        {
            var xml = XElement.Parse(@"<AutoDI GenerateRegistrations=""False"" />");

            var settings = Settings.Parse(new Settings(), xml);

            Assert.IsFalse(settings.GenerateRegistrations);
        }
    }
}
