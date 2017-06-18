using System.Reflection;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using AutoDI.Container.Fody;
using ManualInjectionNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class ManualInjectionOfContainer
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            //Add AutoDI reference
            gen.AddReference(typeof(DependencyAttribute).Assembly.Location);
            gen.AddWeaver("AutoDI");
            gen.AddWeaver("AutoDI.Container");

            _testAssembly = await gen.Execute();
        }

        [TestMethod]
        public void CanManuallyInjectTheGeneratedContainer()
        {
            AutoDIContainer.Inject();

            dynamic sut = _testAssembly.CreateInstance<Sut>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

//<code_file>
namespace ManualInjectionNamespace
{
    using AutoDI;
    using System;

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
//</code_file>