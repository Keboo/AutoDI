using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable RedundantNameQualifier
//<assembly />
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
//<raw:[assembly:AutoDI.Settings(Behavior = AutoDI.Behaviors.None)] />
[assembly: AutoDI.Map("regex:.*", "$0")]
[assembly: AutoDI.Map(@"regex:(.*)\.I(.+)", "$1.$2")]
[assembly: AutoDI.Map(@"IService4", "Service4", Force = true)]
[assembly: AutoDI.Map(@"Service5", "Service5Extended", AutoDI.Lifetime.Scoped)]
[assembly: AutoDI.Map(@"Service2", AutoDI.Lifetime.None)]
[assembly: AutoDI.Map(@"My*", AutoDI.Lifetime.Transient)]
// ReSharper restore RedundantNameQualifier
namespace TestAssembly
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
//</assembly>

namespace AutoDI.Build.Tests
{
    using TestAssembly;

    [TestClass]
    public class ManualMappingTests
    {
        private static Assembly _testAssembly;
        private IContainer _map;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestInitialize]
        public void TestSetup()
        {
            DI.Init(_testAssembly, builder => builder.ConfigureContainer<IContainer>(map => _map = map));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DI.Dispose(_testAssembly);
        }

        [TestMethod]
        public void CanManuallyMapTypes()
        {
            dynamic sut = _testAssembly.CreateInstance<Manager>(typeof(ManualMappingTests));
            Assert.IsTrue(((object)sut.Service).Is<Service>(typeof(ManualMappingTests)));
            Assert.IsNull(sut.Service2);

            var mappings = _map.ToArray();

            Assert.IsFalse(mappings.Any(m => m.SourceType.Is<IService2>(typeof(ManualMappingTests)) || m.TargetType.Is<Service2>(typeof(ManualMappingTests))));
        }

        [TestMethod]
        public void TransientTypesAlwaysCreateNewInstances()
        {
            object dog1 = _testAssembly.Resolve<MyDog>(typeof(ManualMappingTests));
            object dog2 = _testAssembly.Resolve<MyDog>(typeof(ManualMappingTests));

            Assert.IsNotNull(dog1);
            Assert.IsNotNull(dog2);
            Assert.IsFalse(ReferenceEquals(dog1, dog2));
        }

        [TestMethod]
        public void SingletonInstanceAlwaysReturnsTheSameInstance()
        {
            object instance1 = _testAssembly.Resolve<IService>(typeof(ManualMappingTests));
            object instance2 = _testAssembly.Resolve<IService>(typeof(ManualMappingTests));

            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.IsTrue(ReferenceEquals(instance1, instance2));
        }

        [TestMethod]
        public void WhenClassDoesNotImplementInterfaceItIsNotMapped()
        {
            var mappings = _map.ToArray();

            Assert.IsFalse(mappings.Any(m => m.SourceType.Is<IService3>(typeof(ManualMappingTests)) && m.TargetType.Is<Service3>(typeof(ManualMappingTests))));
            Assert.IsTrue(mappings.Any(m => m.SourceType.Is<Service3>(typeof(ManualMappingTests)) && m.TargetType.Is<Service3>(typeof(ManualMappingTests))));
        }

        [TestMethod]
        public void CanForceMappingWhenClassDoesNotImplementInterface()
        {
            var mappings = _map.ToArray();

            Assert.IsTrue(mappings.Any(m => m.SourceType.Is<IService4>(typeof(ManualMappingTests)) && m.TargetType.Is<Service4>(typeof(ManualMappingTests))));
            Assert.IsTrue(mappings.Any(m => m.SourceType.Is<Service4>(typeof(ManualMappingTests)) && m.TargetType.Is<Service4>(typeof(ManualMappingTests))));

            Assert.IsTrue(_testAssembly.Resolve<IService4>(typeof(ManualMappingTests)).Is<Service4>(typeof(ManualMappingTests)));
        }

        [TestMethod]
        public void CanMapFromBaseClassToDerivedClass()
        {
            Assert.IsTrue(_testAssembly.Resolve<Service5>(typeof(ManualMappingTests)).Is<Service5Extended>(typeof(ManualMappingTests)));
        }

        [TestMethod]
        [Description("Issue 59")]
        public void CanDeclareLifetimeInMap()
        {
            var mappings = _map.ToArray();

            var map = mappings.Single(m => m.SourceType.Name == nameof(Service5) && m.TargetType.Name == nameof(Service5Extended));

            Assert.AreEqual(Lifetime.Scoped, map.Lifetime);
        }
    }
}