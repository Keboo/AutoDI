using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoDI.Fody.Tests
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
            gen.WeaverAdded += (sender, args) =>
            {
                if (args.Weaver.Name == "AutoDI")
                {
                    args.Weaver.Instance.Config = XElement.Parse($@"
    <AutoDI Behavior=""{Behaviors.None}"">
        <map from=""regex:.*"" to=""$0"" />
        <map from=""regex:(.*)\.I(.+)"" to=""$1.$2"" />
        <map from=""IService4"" to=""Service4"" force=""true"" />
        <map from=""Service5"" to=""Service5Extended"" Lifetime=""{Lifetime.Scoped}"" />

        <type name=""Service2"" Lifetime=""{Lifetime.None}"" />
        <type name=""My*"" Lifetime=""{Lifetime.Transient}"" />
    </AutoDI>");
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestInitialize]
        public void TestSetup()
        {
            DI.Init(_testAssembly, builder =>
            {
                builder.ConfigureContainer<IContainer>(map =>
                {
                    _map = map;
                });
            });
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
            var mapings = _map.ToArray();

            Assert.IsFalse(mapings.Any(m => m.SourceType.Is<IService3>(typeof(ManualMappingTests)) && m.TargetType.Is<Service3>(typeof(ManualMappingTests))));
            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<Service3>(typeof(ManualMappingTests)) && m.TargetType.Is<Service3>(typeof(ManualMappingTests))));
        }

        [TestMethod]
        public void CanForceMappingWhenClassDoesNotImplementInterface()
        {
            var mapings = _map.ToArray();

            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<IService4>(typeof(ManualMappingTests)) && m.TargetType.Is<Service4>(typeof(ManualMappingTests))));
            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<Service4>(typeof(ManualMappingTests)) && m.TargetType.Is<Service4>(typeof(ManualMappingTests))));
            
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
            var mapings = _map.ToArray();

            var map = mapings.Single(m => m.SourceType.Name == nameof(Service5) && m.TargetType.Name == nameof(Service5Extended));

            Assert.AreEqual(Lifetime.Scoped, map.Lifetime);
        }
    }

    //<assembly />
    //<ref: AutoDI />
    //<weaver: AutoDI />
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
}


