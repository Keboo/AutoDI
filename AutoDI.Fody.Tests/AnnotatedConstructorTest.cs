using System;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using AutoDI.Fody.Tests.AnnotatedConstructorTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Fody.Tests
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
            Assert.IsTrue((service1).Is<Service1>(GetType()));
            Assert.IsNull(service2);
        }

        [TestMethod]
        public void CannotResolveMultipleAnnotatedConstructor()
        {
            dynamic manager = _testAssembly.Resolve<MultipleDiAnnotatedManager>(GetType());
            Assert.IsNull(manager);
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI />
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

        public class MultipleDiAnnotatedManager
        {
            public IService1 Service1 { get; }


            [DiConstructor]
            public MultipleDiAnnotatedManager()
            {
            }

            [DiConstructor]
            public MultipleDiAnnotatedManager(IService1 service1)
            {
                Service1 = service1;
            }
        }
    }
    //</assembly>
}