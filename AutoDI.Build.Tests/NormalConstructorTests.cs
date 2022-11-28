using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NormalConstructorTestsNamespace;

//<assembly>
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
//<raw: [assembly:AutoDI.Settings(DebugCodeGeneration = AutoDI.CodeLanguage.CSharp)]/>
namespace NormalConstructorTestsNamespace
{
    public interface IService
    { }

    public class Service : IService
    { }

    public class Manager
    {
        public IService Service { get; }

        public Manager(IService service)
        {
            Service = service;
        }
    }
}
//</assembly>

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class NormalConstructorTests
    {
        private static Assembly _testAssembly = null!;
        private static bool _initialized = false;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            Generator gen = new();

            _testAssembly = (await gen.Execute()).SingleAssembly();

            DI.Init(_testAssembly);
            _initialized = true;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (_initialized)
            {
                DI.Dispose(_testAssembly);
            }
        }

        [TestMethod]
        public void CanResolveNormalConstructorDependencies()
        {
            dynamic manager = _testAssembly.Resolve<Manager>(GetType());

            Assert.IsTrue(((object)manager.Service).Is<Service>(GetType()));
        }
    }
}