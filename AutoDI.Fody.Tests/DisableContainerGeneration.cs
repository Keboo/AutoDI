using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoDI.AssemblyGenerator;
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
                    weaver.Config = XElement.Parse(@"<AutoDI GenerateContainer=""False"" />");
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void WhenGenerateContainerIsFalseTheContainerIsNotGenerated()
        {
            Assert.Fail("Does this make sense?");
            DI.GetMap(_testAssembly);
        }
    }

    //<assembly />
    //<type:ConsoleApplication/>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace DisableContainerGenerationNamespace
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