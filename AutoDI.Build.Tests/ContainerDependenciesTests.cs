using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using ContainerDependencyNamespace;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
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
            _testAssembly.InvokeStatic<Program>(nameof(Program.Main), new object[] { Array.Empty<string>() });
            dynamic sut = _testAssembly.CreateInstance<Sut>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

//<assembly>
//<type: ConsoleApplication />
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
namespace ContainerDependencyNamespace
{
    using System;

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
            Service = service ?? throw new ArgumentNullException(nameof(service));

            SomeLambda(x => { });
        }

        private static void SomeLambda(Action<int> doStuff)
        {
            if (doStuff is null) throw new ArgumentNullException(nameof(doStuff));
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>