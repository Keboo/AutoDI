using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    using AutoDI;

    [TestClass]
    public class DisableContainerGeneration
    {
        private static Assembly _testAssembly = null!;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();
            _testAssembly.InvokeEntryPoint();
        }

        [TestMethod]
        public void WhenGenerateRegistrationsIsFalseResolutionFails()
        {
            Assert.IsNull(_testAssembly.GetType($"{Constants.Namespace}.{Constants.TypeName}"));
        }
    }

    //<assembly />
    //<type:ConsoleApplication/>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    //<raw:[assembly:AutoDI.Settings(GenerateRegistrations = false)]/>
    namespace DisableGeneratedRegistrationsNamespace
    {
        public class Program
        {
            public static void Main(string[] _)
            { }
        }

        public interface IService { }

        public class Service : IService { }
    }
    //</assembly>
}