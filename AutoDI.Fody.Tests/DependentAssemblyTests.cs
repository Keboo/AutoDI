using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Fody.Tests
{
    using SharedAssembly;
    using MainAssembly;

    [TestClass]
    public class DependentAssemblyTests
    {
        //private static Assembly _sharedAssembly;
        private static Assembly _mainAssembly;
        private static ContainerMap _map;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            var testAssemblies = await gen.Execute();
            //_sharedAssembly = testAssemblies["shared"].Assembly;
            _mainAssembly = testAssemblies["main"].Assembly;

            DI.Init(_mainAssembly, builder =>
            {
                builder.ConfigureContinaer<ContainerMap>(map =>
                {
                    _map = map;
                });
            });
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose();
        }

        [TestMethod]
        public void CanLoadTypesFromDependentAssemblies()
        {
            Assert.IsNotNull(_map);
            Assert.IsTrue(_map.IsMapped<IService, Service>(typeof(DependentAssemblyTests)));
            Assert.IsTrue(_map.IsMapped<Service, Service>(typeof(DependentAssemblyTests)));
            Assert.IsTrue(_map.IsMapped<Manager, Manager>(typeof(DependentAssemblyTests)));
            Assert.IsTrue(_map.IsMapped<Program, Program>(typeof(DependentAssemblyTests)));
        }
    }

    //<assembly:shared />
    //<ref: AutoDI/>
    //<weaver: AutoDI />
    namespace SharedAssembly
    {
        using AutoDI;

        public interface IService { }

        public class Service : IService { }

        public class Manager
        {
            public Manager([Dependency] IService service = null)
            { }
        }
    }

    //<assembly:main />
    //<type:consoleApplication />
    //<ref: shared />
    //<weaver: AutoDI />
    namespace MainAssembly
    {
        using SharedAssembly;

        public class Program
        {
            public static void Main(string[] args)
            {
                var manager = new Manager();
            }
        }
    }
    //</assembly>
}



