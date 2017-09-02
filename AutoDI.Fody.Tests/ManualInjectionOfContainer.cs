using AutoDI.AssemblyGenerator;
using ManualInjectionNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class ManualInjectionOfContainer
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
                    weaver.Config = XElement.Parse(@"<AutoDI AutoInit=""False"" />");
                }
            };
            
            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestMethod]
        public void CanManuallyInjectTheGeneratedContainer()
        {
            //Invoke the entry point, since this is where the automatic injdection would occur
            _testAssembly.InvokeEntryPoint();

            try
            {
                _testAssembly.CreateInstance<Sut>();
                Assert.Fail("Excepted an exception");
            }
            catch (TargetInvocationException e) 
                when (e.InnerException is AutoDINotInitializedException)
            {
            }

            DI.Init(_testAssembly);

            dynamic sut = _testAssembly.CreateInstance<Sut>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

//<assembly />
//<type:ConsoleApplication/>
//<ref: AutoDI />
//<weaver: AutoDI />
namespace ManualInjectionNamespace
{
    using AutoDI;

    public class Program
    {
        public static void Main(string[] args)
        { }
    }

    public class Sut
    {
        public IService Service { get; }

        public Sut([Dependency] IService service = null)
        {
            Service = service;
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>