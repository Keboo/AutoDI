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
        private static Assembly _sharedAssembly;
        private static Assembly _mainAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            var testAssemblies = await gen.Execute();
            _sharedAssembly = testAssemblies["shared"].Assembly;
            _mainAssembly = testAssemblies["main"].Assembly;
        }

        [TestMethod]
        public void CanLoadTypesFromDependentAssemblies()
        {
            ContainerMap map = AutoDIContainer.GetMap(_mainAssembly);
            Assert.IsNotNull(map);
            Assert.IsTrue(map.IsMapped<IService, Service>(typeof(DependentAssemblyTests)));
            Assert.IsTrue(map.IsMapped<Service, Service>(typeof(DependentAssemblyTests)));
            Assert.IsTrue(map.IsMapped<Manager, Manager>(typeof(DependentAssemblyTests)));
            Assert.IsTrue(map.IsMapped<Program, Program>(typeof(DependentAssemblyTests)));
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



