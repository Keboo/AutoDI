using AutoDI.AssemblyGenerator;
using AutoDI.Build.Tests.AnnotatedConstructorTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class AnnotatedConstructorTest
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();

            DI.Init(_testAssembly);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose(_testAssembly);
        }

        [TestMethod]
        public void CanResolveAnnotatedConstructor()
        {
            dynamic manager = _testAssembly.Resolve<SingleDiAnnotatedManager>(GetType());
            var service1 = (object) manager.Service1;
            var service2 = (object) manager.Service2;

            Assert.IsNotNull(service1);
            Assert.IsTrue(service1.Is<Service1>(GetType()));
            Assert.IsNull(service2);
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace AnnotatedConstructorTestsNamespace
    {
        using AutoDI;

        public interface IService1
        { }

        public class Service1 : IService1
        { }

        public interface IService2
        { }

        public class Service2 : IService2
        { }

        public class SingleDiAnnotatedManager
        {
            public IService1 Service1 { get; }

            public IService2 Service2 { get; }


            [DiConstructor]
            public SingleDiAnnotatedManager(IService1 service1)
            {
                Service1 = service1;
                Service2 = null;
            }

            public SingleDiAnnotatedManager(IService1 service1, IService2 service2)
            {
                Service1 = service1;
                Service2 = service2;
            }
        }
    }
    //</assembly>
}