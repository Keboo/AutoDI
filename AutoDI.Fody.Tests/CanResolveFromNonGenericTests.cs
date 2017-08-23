using AutoDI.AssemblyGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Fody.Tests
{
    using CanResolveFromNonGenericNamespace;

    [TestClass]
    public class CanResolveFromNonGenericTests
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
            DI.Dispose();
        }

        [TestMethod]
        [Description("Issue 26")]
        public void CanResolveServiceWithNonGenericMethod()
        {
            _testAssembly.InvokeEntryPoint();
            DI.Global.GetService(typeof(IService)).Is<Service>();
        }

        [TestMethod]
        [Description("Issue 26")]
        public void CanResolveServiceWithGenericMethod()
        {
            _testAssembly.InvokeEntryPoint();
            DI.Global.GetService<IService>().Is<Service>();
        }
    }

    //<assembly />
    //<type:ConsoleApplication/>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace CanResolveFromNonGenericNamespace
    {
        public class Program
        {
            public static void Main(string[] args)
            { }
        }

        public interface IService { }

        public class Service : IService { }
    }
    //</assembly>
}