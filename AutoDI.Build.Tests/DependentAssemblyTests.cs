extern alias AutoDIBuild;

using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    using MainAssembly;

    using SharedAssembly;

    [TestClass]
    public class DependentAssemblyTests
    {
        private static Assembly _mainAssembly = null!;
        private static bool _initialized;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            var gen = new Generator();

            var testAssemblies = await gen.Execute();

            _mainAssembly = testAssemblies["main"].Assembly ?? throw new Exception("Could not find main assembly");
            _initialized = true;
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_initialized)
            {
                DI.Dispose(_mainAssembly);
            }
        }

        [TestMethod]
        public void CanLoadTypesFromDependentAssemblies()
        {
            IContainer? map = null;
            DI.Init(_mainAssembly, builder => builder.ConfigureContainer<IContainer>(container => map = container));

            Assert.IsNotNull(map);
            Assert.IsTrue(map.IsMapped<IService, Service>(GetType()));
            Assert.IsTrue(map.IsMapped<Service, Service>(GetType()));
            Assert.IsTrue(map.IsMapped<Manager, Manager>(GetType()));
            Assert.IsTrue(map.IsMapped<Program, Program>(GetType()));
        }

        [TestMethod]
        public void DependenciesInReferencedAssembliesResolve()
        {
            _mainAssembly.InvokeEntryPoint();
            dynamic manager = _mainAssembly.GetStaticProperty<Program>(nameof(Program.Manager), GetType());
            Assert.IsNotNull(manager);
            Assert.IsNotNull(manager.Service);
        }
    }

    //<assembly:shared />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    //<ref: AutoDI/>
    namespace SharedAssembly
    {
        using AutoDI;

        public interface IService { }

        public class Service : IService { }

        public class Manager
        {
            public IService Service { get; }

            public Manager([Dependency] IService service = null!)
            {
                Service = service;
            }
        }
    }

    //<assembly:main />
    //<type:consoleApplication />
    //<ref: shared />
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace MainAssembly
    {
        using AutoDI;

        using SharedAssembly;

        public class Program
        {
            public static Manager? Manager { get; set; }

            public static void Main(string[] _)
            {
                Manager = new Manager();
            }

            public Program([Dependency] IService service = null!)
            {

            }
        }
    }
    //</assembly>
}