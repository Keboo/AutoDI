using AutoDI.AssemblyGenerator;
using AutoDI.Fody.Tests.BehaviorsTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class BehaviorsTests
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose(_testAssembly);
        }

        [TestMethod]
        public void ClassesShouldDefaultToResolvingToThemselves()
        {
            DI.Init(_testAssembly);

            Assert.IsTrue(_testAssembly.Resolve<LibraryClass>(GetType()).Is<LibraryClass>(GetType()));
            Assert.IsTrue(_testAssembly.Resolve<MyBaseClass>(GetType()).Is<MyBaseClass>(GetType()));
            Assert.IsTrue(_testAssembly.Resolve<MyClass>(GetType()).Is<MyClass>(GetType()));
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace BehaviorsTestsNamespace
    {
        public class LibraryClass {}

        public class MyBaseClass : LibraryClass {}

        public class MyClass : MyBaseClass {}
    }
    //</assembly>
}