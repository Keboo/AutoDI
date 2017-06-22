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
            var xml = XElement.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <Weavers>
                    <AutoDI/>
                    <AutoDI.Container />
                </Weavers >");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(Behaviors.Default, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadIndividualBehavior()
        {
            var xml = XElement.Parse($@"<?xml version=""1.0"" encoding=""utf-8""?>
                <Weavers>
                    <AutoDI/>
                    <AutoDI.Container Behavior=""{Behaviors.SingleInterfaceImplementation}""/>
                </Weavers >");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(Behaviors.SingleInterfaceImplementation, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadCompositeBehavior()
        {
            var xml = XElement.Parse($@"<?xml version=""1.0"" encoding=""utf-8""?>
                <Weavers>
                    <AutoDI/>
                    <AutoDI.Container Behavior=""{Behaviors.SingleInterfaceImplementation},{Behaviors.IncludeClasses}""/>
                </Weavers >");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(Behaviors.SingleInterfaceImplementation | Behaviors.IncludeClasses, settings.Behavior);
        }

        [TestMethod]
        public void CanLoadSettingsWithDeclaredType()
        {
            var xml = XElement.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <Weavers>
                    <AutoDI/>
                    <AutoDI.Container>
                        <type name=""MyType.*"" Create=""Transient"" />
                    </AutoDI.Container>
                </Weavers >");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(1, settings.Types.Count);
            Assert.IsTrue(settings.Types[0].Matches("MyType2"));
            Assert.AreEqual(Create.Transient, settings.Types[0].Create);
        }

        [TestMethod]
        public void CanLoadSettingsWithSimpleMap()
        {
            var xml = XElement.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <Weavers>
                    <AutoDI/>
                    <AutoDI.Container>
                        <map from=""IService"" to=""Service"" />
                    </AutoDI.Container>
                </Weavers >");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(1, settings.Maps.Count);
            string mappedType;
            Assert.IsTrue(settings.Maps[0].TryGetMap("IService", out mappedType));
            Assert.AreEqual("Service", mappedType);
        }

        [TestMethod]
        public void CanLoadSettingsWithRegexMap()
        {
            var xml = XElement.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <Weavers>
                    <AutoDI/>
                    <AutoDI.Container>
                        <map from=""ViewModels.(.*)"" to=""Views.$1"" />
                    </AutoDI.Container>
                </Weavers >");

            var settings = Settings.Parse(xml);

            Assert.AreEqual(1, settings.Maps.Count);
            string mappedType;
            Assert.IsTrue(settings.Maps[0].TryGetMap("ViewModels.Test", out mappedType));
            Assert.AreEqual("Views.Test", mappedType);
        }
    }
}
