using AutoDI.AssemblyGenerator;
using AutoDI.Build.Tests.GenericClassesTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class GenericClassTests
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
        [Description("Issue 59")]
        public void OpenGenericIsNotMapped()
        {
            object service = _testAssembly.Resolve<IService>(GetType());

            Assert.IsNull(service);
        }

        [TestMethod]
        [Description("Issue 59")]
        public void ClosedGenericIsMapped()
        {
            object service = _testAssembly.Resolve<Service2>(GetType());

            Assert.IsTrue(service.Is<Service2>(GetType()));
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace GenericClassesTestsNamespace
    {
        public interface IService { }

        public class OpenService<T> : IService
        {
        }

        public interface IService2 { }

        public class BaseGeneric<T> : IService2 { }

        public class Service2 : BaseGeneric<int> { }
    }
    //</assembly>
}
