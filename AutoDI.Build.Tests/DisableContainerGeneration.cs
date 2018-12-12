﻿extern alias AutoDIBuild;

using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

using Settings = AutoDIBuild::AutoDI.Build.Settings;

namespace AutoDI.Build.Tests
{
    using AutoDI;

    [TestClass]
    public class DisableContainerGeneration
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            gen.WeaverAdded += (sender, args) =>
            {
                args.Weaver.Config = XElement.Parse($@"<AutoDI {nameof(Settings.GenerateRegistrations)}=""False"" />");
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();
            _testAssembly.InvokeEntryPoint();
        }

        [TestMethod]
        public void WhenGenerateRegistrationsIsFalseResolutionFails()
        {
            Assert.IsNull(_testAssembly.GetType($"{Constants.Namespace}.{Constants.TypeName}"));
        }
    }

    //<assembly />
    //<type:ConsoleApplication/>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace DisableGeneratedRegistrationsNamespace
    {
        public class Program
        {
            public static void Main(string[] args)
            { }
        }

        public interface IService { }

        public class Service : IService { }
    }
    //</assembly>
}