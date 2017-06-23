using AutoDI.AssemblyGenerator;
using AutoDI.Container.Fody;
using ManualMappingTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        <map from=""(.*)\.I(.+)"" to=""$1.$2"" />
        <map from="".*"" to=""$0"" />
        <map from=""IService4"" to=""Service4"" force=""true"" />
        <map from=""Service5"" to=""Service5Extended"" />

        <type name=""Service2"" Create=""{Create.None}"" />
        <type name=""My.*"" Create=""{Create.Transient}"" />
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

            var map = AutoDIContainer.GetMap(_testAssembly);
            var mappings = map.GetMappings().ToArray();

            Assert.IsFalse(mappings.Any(m => m.SourceType.Is<IService2>() || m.TargetType.Is<Service2>()));
        }

        [TestMethod]
        public void TransientTypesAlwaysCreateNewInstances()
        {
            object dog1 = _testAssembly.Resolve<MyDog>();
            object dog2 = _testAssembly.Resolve<MyDog>();

            Assert.IsNotNull(dog1);
            Assert.IsNotNull(dog2);
            Assert.IsFalse(ReferenceEquals(dog1, dog2));
        }

        [TestMethod]
        public void SingletonInstanceAlwaysReturnsTheSameInstance()
        {
            object instance1 = _testAssembly.Resolve<IService>();
            object instance2 = _testAssembly.Resolve<IService>();

            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.IsTrue(ReferenceEquals(instance1, instance2));
        }

        [TestMethod]
        public void WhenClassDoesNotImplementInterfaceItIsNotMapped()
        {
            var map = AutoDIContainer.GetMap(_testAssembly);
            var mapings = map.GetMappings().ToArray();
            
            Assert.IsFalse(mapings.Any(m => m.SourceType.Is<IService3>() && m.TargetType.Is<Service3>()));
            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<Service3>() && m.TargetType.Is<Service3>()));
        }

        [TestMethod]
        public void CanForceMappingWhenClassDoesNotImplementInterface()
        {
            var map = AutoDIContainer.GetMap(_testAssembly);
            var mapings = map.GetMappings().ToArray();

            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<IService4>() && m.TargetType.Is<Service4>()));
            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<Service4>() && m.TargetType.Is<Service4>()));

            //Just because you can map them, doesn't mean you can actually get anything back!
            Assert.IsNull(_testAssembly.Resolve<IService4>());
        }

        [TestMethod]
        public void CanMapFromBaseClassToDerivedClass()
        {
            Assert.IsTrue(_testAssembly.Resolve<Service5>().Is<Service5Extended>());
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

    public class Service2 : IService2
    { }

    public interface IService3 { }

    public class Service3 { }

    public interface IService4 { }

    public class Service4 { }

    public class Service5 { }

    public class Service5Extended : Service5 { }

    public class MyDog { }

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
