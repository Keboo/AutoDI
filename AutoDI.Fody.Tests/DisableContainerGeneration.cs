using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class DisableContainerGeneration
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            gen.WeaverAdded += (sender, args) =>
            {
                if (args.Weaver.Name == "AutoDI")
                {
                    args.Weaver.Instance.Config = XElement.Parse($@"<AutoDI {nameof(Settings.GenerateRegistrations)}=""False"" />");
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();
            _testAssembly.InvokeEntryPoint();
        }

        [TestMethod]
        public void WhenGenerateRegistrationsIsFalseResolutionFails()
        {
            Assert.IsNull(_testAssembly.GetType($"{DI.Namespace}.{DI.TypeName}"));
        }
    }

    //<assembly />
    //<type:ConsoleApplication/>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace DisableGeneratedRegistrationsNamespace
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