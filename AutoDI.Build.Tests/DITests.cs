using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class DITests
    {
        private static Assembly _testAssembly = null!;
        private static bool _initialized = false;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();
            _initialized = true;
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_initialized)
            {
                DI.Dispose(_testAssembly);
            }
        }

        [TestMethod]
        public void InitThrowsIfCalledTwice()
        {
            DI.Init(_testAssembly);

            try
            {
                DI.Init(_testAssembly);
            }
            catch (TargetInvocationException e)
                when (e.InnerException is AlreadyInitializedException)
            {
                return;
            }
            Assert.Fail($"Excepted {nameof(AlreadyInitializedException)}");
        }

        [TestMethod]
        public void TryInitReturnsFalseOnSecondInvocation()
        {
            Assert.IsTrue(DI.TryInit(_testAssembly));
            Assert.IsFalse(DI.TryInit(_testAssembly));
            DI.Dispose(_testAssembly);
            Assert.IsTrue(DI.TryInit(_testAssembly));
            Assert.IsFalse(DI.TryInit(_testAssembly));
        }
    }


    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace DITestsNamespace
    {

    }
    //</assembly>
}