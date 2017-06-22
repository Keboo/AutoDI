using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoDI.AssemblyGenerator;
using AutoDI.Container.Fody;
using ManualMappingTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class ManualMappingTests
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            //Add AutoDI reference
            gen.AddReference(typeof(DependencyAttribute).Assembly.Location);
            gen.AddWeaver("AutoDI");
            dynamic container = gen.AddWeaver("AutoDI.Container");
            
            container.Config = XElement.Parse($@"<?xml version=""1.0"" encoding=""utf-8""?>
<Weavers>
    <AutoDI/>
    <AutoDI.Container Behavior=""{Behaviors.None}"">
        <type name=""MyType.*"" Create=""{Create.Transient}"" />
        <map from=""IService"" to=""Service"" />
    </AutoDI.Container>
</Weavers >");
            _testAssembly = await gen.Execute();
        }

        [TestMethod]
        public void CanManuallyMapTypes()
        {
            AutoDIContainer.Inject(_testAssembly);

            dynamic sut = _testAssembly.CreateInstance<Manager>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
            Assert.IsNull(sut.Service2);
        }
    }
}

//<gen>
namespace ManualMappingTests
{
    using AutoDI;

    public interface IService
    { }

    public class Service : IService
    { }

    public interface IService2 { }

    public class Service2 : IService
    { }

    public class Manager
    {
        public Manager([Dependency] IService service = null, [Dependency] IService2 service2 = null)
        {
            Service = service;
            Service2 = service2;
        }

        public IService Service { get; }

        public IService2 Service2 { get; }


    }
}
//</gen>
