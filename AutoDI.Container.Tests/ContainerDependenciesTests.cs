using AutoDI.AssemblyGenerator;
using ContainerDependencyNameSpace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;


namespace AutoDI.Container.Tests
{
    [TestClass]
    public class ContainerDependenciesTests
    {
        private static Assembly _testAssembly;
        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator(AssemblyType.ConsoleApplication);
            
            //Add AutoDI reference
            gen.AddReference(typeof(DependencyAttribute).Assembly.Location);
            gen.AddWeaver("AutoDI");
            gen.AddWeaver("AutoDI.Container");

            _testAssembly = await gen.Execute();
        }

        [TestMethod]
        public void SimpleConstructorDependenciesAreInjected()
        {
            _testAssembly.InvokeStatic<Program>(nameof(Program.Main), new object[] {new string[0]});
            dynamic sut = _testAssembly.CreateInstance<Sut>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

//<gen>
namespace ContainerDependencyNameSpace
{
    using AutoDI;
    using System;

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
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</gen>
