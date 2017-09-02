using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoDI.AssemblyGenerator;
using AutoDI.Fody.Tests.DisableGeneratedRegistrationsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Fody.Tests
{
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
                if (args.Weaver.Name == "AutoDI")
                {
                    dynamic weaver = args.Weaver;
                    weaver.Config = XElement.Parse($@"<AutoDI {nameof(Settings.GenerateRegistrations)}=""False"" />");
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();
            _testAssembly.InvokeEntryPoint();
        }

        [TestMethod]
        public void WhenGenerateRegistrationsIsFalseResolutionFails()
        {
            Assert.IsNull(_testAssembly.Resolve<Service>(GetType()));
        }
    }

    //<assembly />
    //<type:ConsoleApplication/>
    //<ref: AutoDI />
    //<weaver: AutoDI />
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