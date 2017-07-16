using AutoDI.AssemblyGenerator;
using AutoDI.Fody;
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

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            gen.WeaverAdded += (sender, args) =>
            {
                if (args.Weaver.Name == "AutoDI.Container")
                {
                    dynamic containerWeaver = args.Weaver;
                    containerWeaver.Config = XElement.Parse($@"
    <AutoDI.Container Behavior=""{Behaviors.None}"">
        <map from=""(.*)\.I(.+)"" to=""$1.$2"" />
        <map from="".*"" to=""$0"" />
        <map from=""IService4"" to=""Service4"" force=""true"" />
        <map from=""Service5"" to=""Service5Extended"" />

        <type name=""Service2"" Lifetime=""{Lifetime.None}"" />
        <type name=""My.*"" Lifetime=""{Lifetime.Transient}"" />
    </AutoDI.Container>");
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestMethod]
        public void CanManuallyMapTypes()
        {
            AutoDIContainer.Inject(_testAssembly);

            dynamic sut = _testAssembly.CreateInstance<Manager>(typeof(ManualMappingTests));
            Assert.IsTrue(((object)sut.Service).Is<Service>(typeof(ManualMappingTests)));
            Assert.IsNull(sut.Service2);

            var map = AutoDIContainer.GetMap(_testAssembly);
            var mappings = map.GetMappings().ToArray();

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
            var map = AutoDIContainer.GetMap(_testAssembly);
            var mapings = map.GetMappings().ToArray();
            
            Assert.IsFalse(mapings.Any(m => m.SourceType.Is<IService3>(typeof(ManualMappingTests)) && m.TargetType.Is<Service3>(typeof(ManualMappingTests))));
            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<Service3>(typeof(ManualMappingTests)) && m.TargetType.Is<Service3>(typeof(ManualMappingTests))));
        }

        [TestMethod]
        public void CanForceMappingWhenClassDoesNotImplementInterface()
        {
            var map = AutoDIContainer.GetMap(_testAssembly);
            var mapings = map.GetMappings().ToArray();

            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<IService4>(typeof(ManualMappingTests)) && m.TargetType.Is<Service4>(typeof(ManualMappingTests))));
            Assert.IsTrue(mapings.Any(m => m.SourceType.Is<Service4>(typeof(ManualMappingTests)) && m.TargetType.Is<Service4>(typeof(ManualMappingTests))));

            //Just because you can map them, doesn't mean you can actually get anything back!
            Assert.IsNull(_testAssembly.Resolve<IService4>(typeof(ManualMappingTests)));
        }

        [TestMethod]
        public void CanMapFromBaseClassToDerivedClass()
        {
            Assert.IsTrue(_testAssembly.Resolve<Service5>(typeof(ManualMappingTests)).Is<Service5Extended>(typeof(ManualMappingTests)));
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


