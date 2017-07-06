using AutoDI.Container.Fody;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void CanLoadBasicSettings()
        {
            var xml = XElement.Parse(@"<AutoDI.Container />");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(Behaviors.Default, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadIndividualBehavior()
        {
            var xml = XElement.Parse($@"<AutoDI.Container behavior=""{Behaviors.SingleInterfaceImplementation}""/>");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(Behaviors.SingleInterfaceImplementation, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadCompositeBehavior()
        {
            var xml = XElement.Parse($@"<AutoDI.Container behavior=""{Behaviors.SingleInterfaceImplementation},{Behaviors.IncludeClasses}""/>");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(Behaviors.SingleInterfaceImplementation | Behaviors.IncludeClasses, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadSettingsWithDeclaredType()
        {
            var xml = XElement.Parse(@"
                    <AutoDI.Container>
                        <type name=""MyType.*"" lifetime=""Transient"" />
                    </AutoDI.Container>");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(1, settings.Types.Count);
            Assert.IsTrue(settings.Types[0].Matches("MyType2"));
            Assert.AreEqual(Lifetime.Transient, settings.Types[0].Lifetime);
        }

        [TestMethod]
        public void CanLoadSettingsWithSimpleMap()
        {
            var xml = XElement.Parse(@"
                    <AutoDI.Container>
                        <map from=""IService"" to=""Service"" />
                    </AutoDI.Container>");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(1, settings.Maps.Count);
            string mappedType;
            Assert.IsTrue(settings.Maps[0].TryGetMap("IService", out mappedType));
            Assert.AreEqual("Service", mappedType);
        }

        [TestMethod]
        public void CanLoadSettingsWithRegexMap()
        {
            var xml = XElement.Parse(@"
                    <AutoDI.Container>
                        <map from=""ViewModels.(.*)"" to=""Views.$1"" />
                    </AutoDI.Container>");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(1, settings.Maps.Count);
            string mappedType;
            Assert.IsTrue(settings.Maps[0].TryGetMap("ViewModels.Test", out mappedType));
            Assert.AreEqual("Views.Test", mappedType);
        }

        [TestMethod]
        public void CanLoadSettingsWithIncludedAssembly()
        {
            var xml = XElement.Parse(@"
                    <AutoDI.Container>
                        <assembly name=""MyAssembly.*"" />
                    </AutoDI.Container>");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(1, settings.Assemblies.Count);
            Assert.IsTrue(settings.Assemblies[0].Matches("MyAssembly.Test"));
        }
    }
}
