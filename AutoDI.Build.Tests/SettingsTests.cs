extern alias AutoDIBuild;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using Mono.Cecil;

using Settings=AutoDIBuild::AutoDI.Build.Settings;
using SettingsParseException=AutoDIBuild::AutoDI.Build.SettingsParseException;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void DebugCodeGenerationDefaultsOff()
        {
            var settings = new Settings();

            Assert.AreEqual((int)CodeLanguage.None, (int)settings.DebugCodeGeneration);
        }

        [TestMethod]
        public void InitModeDefaultsEntryPoint()
        {
            var settings = new Settings();

            Assert.AreEqual((int)InitMode.EntryPoint, (int)settings.InitMode);
        }
    }
}
