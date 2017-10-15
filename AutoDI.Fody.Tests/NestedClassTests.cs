using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.Fody.Tests.NestedClassesTestsNamespace;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class NestedClassTests
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
        [Description("Issue 75")]
        public void NestedClassIsNotMapped()
        {
            dynamic service = _testAssembly.CreateInstance<Service>();
            Assert.IsNotNull(service);
            var nestedServiceName = service.GetNested().GetType().Name;
            Assert.AreEqual("NestedService", nestedServiceName);
            var twiceNested = service.GetTwiceNested().GetType().Name;
            Assert.AreEqual("TwiceNested", twiceNested);
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace NestedClassesTestsNamespace
    {
        public class Service
        {
            public object GetNested() => new NestedService();
            public object GetTwiceNested() => new OtherNestedClass().GetTwiceNested();

            private class NestedService
            {
            }

            private class OtherNestedClass
            {
                public object GetTwiceNested() => new TwiceNested();
                private class TwiceNested
                {
                    
                }
            }
        }
    }
    //</assembly>
}
