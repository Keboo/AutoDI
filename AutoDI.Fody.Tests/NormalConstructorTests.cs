﻿using AutoDI.AssemblyGenerator;
using AutoDI.Fody.Tests.NormalConstructorTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class NormalConstructorTests
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            gen.WeaverAdded += (sender, args) =>
            {
                if (args.Weaver.Name == "AutoDI")
                {
                    args.Weaver.Instance.Config = XElement.Parse($@"<AutoDI DebugCodeGeneration=""CSharp"" />");
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();

            DI.Init(_testAssembly);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose(_testAssembly);
        }

        [TestMethod]
        public void CanResolveNormalConstructorDependencies()
        {
            dynamic manager = _testAssembly.Resolve<Manager>(GetType());

            Assert.IsTrue(((object)manager.Service).Is<Service>(GetType()));
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace NormalConstructorTestsNamespace
    {
        public interface IService
        { }

        public class Service : IService
        { }

        public class Manager
        {
            public IService Service { get; }

            public Manager(IService service)
            {
                Service = service;
            }
        }
    }
    //</assembly>
}