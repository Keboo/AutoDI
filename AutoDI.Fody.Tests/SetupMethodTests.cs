using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class SetupMethodTests
    {
        private static Assembly _publicAssembly;
        private static Assembly _internalAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            Dictionary<string, AssemblyInfo> result = await gen.Execute();
            _publicAssembly = result["public"].Assembly;
            _internalAssembly = result["internal"].Assembly;
        }

        [TestMethod]
        [Description("Issue 23")]
        public void PublicSetupMethodIsInvoked()
        {
            ContainerMap GetInitMap()
            {
                // ReSharper disable once PossibleNullReferenceException
                return (ContainerMap) _publicAssembly.GetType(typeof(SetupMethodPublicTests.Program).FullName)
                    .GetProperty(nameof(SetupMethodPublicTests.Program.InitMap)).GetValue(null);
            }
            
            Assert.IsNull(GetInitMap());
            _publicAssembly.InvokeEntryPoint();
            Assert.IsNotNull(GetInitMap());
        }

        [TestMethod]
        [Description("Issue 38")]
        public void InternalSetupMethodIsInvoked()
        {
            ContainerMap GetInitMap()
            {
                // ReSharper disable once PossibleNullReferenceException
                return (ContainerMap)_internalAssembly.GetType(typeof(SetupMethodInternalTests.Program).FullName)
                    .GetProperty(nameof(SetupMethodInternalTests.Program.InitMap)).GetValue(null);
            }

            Assert.IsNull(GetInitMap());
            _internalAssembly.InvokeEntryPoint();
            Assert.IsNotNull(GetInitMap());
        }
    }
}
//<assembly:public />
//<type: ConsoleApplication />
//<ref: AutoDI.Container />
//<weaver: AutoDI.Container />
namespace SetupMethodPublicTests
{
    using AutoDI;

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

//<assembly:internal />
//<type: ConsoleApplication />
//<ref: AutoDI />
//<weaver: AutoDI />
namespace SetupMethodInternalTests
{
    using AutoDI;

    public class Program
    {
        public static ContainerMap InitMap { get; set; }

        public static void Main(string[] args)
        {

        }

        [SetupMethod]
        internal static void InitializeContainer(ContainerMap map)
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