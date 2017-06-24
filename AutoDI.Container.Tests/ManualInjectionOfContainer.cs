using AutoDI.AssemblyGenerator;
using AutoDI.Container.Fody;
using ManualInjectionNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class ManualInjectionOfContainer
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator(AssemblyType.ConsoleApplication);

            //Add AutoDI reference
            gen.AddReference(typeof(DependencyAttribute).Assembly.Location);
            gen.AddWeaver("AutoDI");
            dynamic container = gen.AddWeaver("AutoDI.Container");

            container.Config = XElement.Parse(@"<AutoDI.Container InjectContainer=""false"" />");

            _testAssembly = await gen.Execute();
        }

        [TestMethod]
        public void CanManuallyInjectTheGeneratedContainer()
        {
            //Invoke the entry point, since this is where the automatic injdection would occur
            _testAssembly.InvokeStatic<Program>(nameof(Program.Main), (object)new string[0]);

            dynamic sut = _testAssembly.CreateInstance<Sut>();
            Assert.IsFalse(((object)sut.Service).Is<Service>());

            AutoDIContainer.Inject(_testAssembly);

            sut = _testAssembly.CreateInstance<Sut>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

//<gen>
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
//</gen>