using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//<assembly>
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
using AutoDI;
using System.Reflection;
using System.Threading.Tasks;

[assembly: Map(Lifetime.Singleton, typeof(MapAttributeTestsNamespace.AssemblySingletonClass))]

namespace MapAttributeTestsNamespace
{

    [Map(Lifetime.Singleton)]
    public class SingletonClass { }

    public class AssemblySingletonClass { }
}
//</assembly>

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class MapAttributeTests
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
        public void CanApplyAttributesToSpecifyLifetimeForClasses()
        {
            var singletonClass1 = _testAssembly.Resolve<MapAttributeTestsNamespace.SingletonClass>();
            var singletonClass2 = _testAssembly.Resolve<MapAttributeTestsNamespace.SingletonClass>();
            
            Assert.IsNotNull(singletonClass1);
            Assert.IsTrue(ReferenceEquals(singletonClass1, singletonClass2));

            var assemblySingletonClass1 = _testAssembly.Resolve<MapAttributeTestsNamespace.AssemblySingletonClass>();
            var assemblySingletonClass2 = _testAssembly.Resolve<MapAttributeTestsNamespace.AssemblySingletonClass>();

            Assert.IsNotNull(assemblySingletonClass1);
            Assert.IsTrue(ReferenceEquals(assemblySingletonClass1, assemblySingletonClass2));
        }
    }
}