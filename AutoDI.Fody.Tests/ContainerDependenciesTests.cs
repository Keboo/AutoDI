using ContainerDependencyNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class ContainerDependenciesTests
    {
        private static Assembly _testAssembly;
        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();
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

//<assembly>
//<type: ConsoleApplication />
//<ref: AutoDI />
//<weaver: AutoDI />
namespace ContainerDependencyNamespace
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

            SomeLambda(x => { });
        }

        private static void SomeLambda(Action<int> doStuff)
        {
            if (doStuff == null) throw new ArgumentNullException(nameof(doStuff));
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>
