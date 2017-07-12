using System.Reflection;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SetupMethodTests;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class SetupMethodTests
    {
        private static Assembly _assembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            _assembly = (await gen.Execute()).SingleAssembly();
        }

        [TestMethod]
        [Description("Issue 23")]
        public void SetupMethodIsInvoked()
        {
            ContainerMap GetInitMap()
            {
                // ReSharper disable once PossibleNullReferenceException
                return (ContainerMap) _assembly.GetType(typeof(Program).FullName).GetProperty(nameof(Program.InitMap)).GetValue(null);
            }
            
            Assert.IsNull(GetInitMap());
            _assembly.InvokeEntryPoint();
            Assert.IsNotNull(GetInitMap());
        }
    }
}
//<assembly />
//<type: ConsoleApplication />
//<ref: AutoDI.Container />
//<weaver: AutoDI.Container />
namespace SetupMethodTests
{
    using AutoDI.Container;

    public class Program
    {
        public static ContainerMap InitMap { get; set; }

        public static void Main(string[] args)
        {
            
        }

        [SetupMethod]
        public static void InitializeContainer(ContainerMap map)
        {
            InitMap = map;
        }
    }

    public class Container
    {
        private static readonly ContainerMap _map;
        static Container()
        {
            _map = new ContainerMap();

            Program.InitializeContainer(_map);
        }
    }
}
//</assembly>