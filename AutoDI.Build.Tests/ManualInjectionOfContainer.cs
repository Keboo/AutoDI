using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using ManualInjectionNamespace;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class ManualInjectionOfContainer
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestMethod]
        public void CanManuallyInjectTheGeneratedContainer()
        {
            //Invoke the entry point, since this is where the automatic injection would occur
            _testAssembly.InvokeEntryPoint();

            dynamic sut;
            try
            {
                //This should throw or return null since AutoDI has not been initialized
                sut = _testAssembly.CreateInstance<Sut>();
                Assert.IsNull(sut.Service);
            }
            catch (TargetInvocationException e)
                when (e.InnerException is NotInitializedException)
            { }


            DI.Init(_testAssembly);

            sut = _testAssembly.CreateInstance<Sut>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

//<assembly />
//<type:ConsoleApplication/>
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
//<raw:[assembly:AutoDI.Settings(InitMode = AutoDI.InitMode.Manual)] />
namespace ManualInjectionNamespace
{
    using AutoDI;

    public class Program
    {
        public static void Main(string[] args)
        { }
    }

    public class Sut
    {
        public IService Service { get; }

        public Sut([Dependency] IService service = null)
        {
            Service = service;
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>