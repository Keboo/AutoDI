using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class DependentAssemblyTests
    {
        private static Assembly _sharedAssembly;
        private static Assembly _mainAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator(AssemblyType.ConsoleApplication);

            var testAssemblies = await gen.Execute2();
            _sharedAssembly = testAssemblies["shared"];
            _mainAssembly = testAssemblies["main"];
        }

        [TestMethod]
        public void CanLoadTypesFromDependentAssemblies()
        {
            
        }
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
//<weaver: AutoDI.Container />
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
