using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ModuleLoadInjectionNamespace;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class ModuleLoadInjectionTests
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
        public void InitThrowsIfModuleLoadAssembly()
        {
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
        public void TryInitReturnsFalseOnFirstInvocation()
        {
            Assert.IsFalse(DI.TryInit(_testAssembly));
        }

        [TestMethod]
        public void LibraryDependenciesAreInjected()
        {
            dynamic sut = _testAssembly.CreateInstance<ModuleLoadingLibrary>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

//<assembly />
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
//<raw:[assembly:AutoDI.Settings(InitMode = AutoDI.InitMode.ModuleLoad)] />
namespace ModuleLoadInjectionNamespace
{
    using AutoDI;

    public class ModuleLoadingLibrary
    {
        public IService Service { get; }

        public ModuleLoadingLibrary([Dependency] IService service = null)
        {
            Service = service;
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>