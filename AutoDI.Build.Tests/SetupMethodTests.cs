using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class SetupMethodTests
    {
        private static Assembly _publicAssembly = null!;
        private static Assembly _internalAssembly = null!;
        private static Assembly _manualAssembly = null!;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            Generator gen = new();
            Dictionary<string, AssemblyInfo> result = await gen.Execute();
            _publicAssembly = result["public"].Assembly ?? throw new Exception("Could not find public assembly");
            _internalAssembly = result["internal"].Assembly ?? throw new Exception("Could not find internal assembly");
            _manualAssembly = result["manual"].Assembly ?? throw new Exception("Could not find manual assembly");
        }

        [TestMethod]
        [Description("Issue 23")]
        public void PublicSetupMethodIsInvoked()
        {
            static ContainerMap GetInitMap()
            {
                // ReSharper disable once PossibleNullReferenceException
                return (ContainerMap)_publicAssembly.GetStaticProperty<SetupMethodPublicTests.Program>(
                    nameof(SetupMethodPublicTests.Program.InitMap));
            }

            Assert.IsNull(GetInitMap());
            _publicAssembly.InvokeEntryPoint();
            Assert.IsNotNull(GetInitMap());
        }

        [TestMethod]
        [Description("Issue 38")]
        public void InternalSetupMethodIsInvoked()
        {
            static ContainerMap GetInitMap()
            {
                // ReSharper disable once PossibleNullReferenceException
                return (ContainerMap)_internalAssembly.GetStaticProperty<SetupMethodInternalTests.Program>(
                    nameof(SetupMethodInternalTests.Program.InitMap));
            }

            Assert.IsNull(GetInitMap());
            _internalAssembly.InvokeEntryPoint();
            Assert.IsNotNull(GetInitMap());
        }

        [TestMethod]
        [Description("Issue 49")]
        public void SetupMethodIsNotInvokedUntilTheContainerIsInjected()
        {
            static ContainerMap GetInitMap()
            {
                // ReSharper disable once PossibleNullReferenceException
                return (ContainerMap)_manualAssembly.GetStaticProperty<SetupMethodManualInjectionTests.TestClass>(
                    nameof(SetupMethodManualInjectionTests.TestClass.InitMap));
            }

            Assert.IsNull(GetInitMap());
            DI.Init(_manualAssembly);
            Assert.IsNotNull(GetInitMap());

        }
    }
}
//<assembly:public />
//<type: ConsoleApplication />
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
namespace SetupMethodPublicTests
{
    using AutoDI;

    public class Program
    {
        public static IContainer? InitMap { get; set; }
        
        public static void Main(string[] _)
        {

        }

        [SetupMethod]
        public static void Setup(IApplicationBuilder builder)
        {
            builder.ConfigureContainer<IContainer>(map => InitMap = map);
        }
    }
}
//</assembly>

//<assembly:internal />
//<type: ConsoleApplication />
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
namespace SetupMethodInternalTests
{
    using AutoDI;

    public class Program
    {
        public static IContainer? InitMap { get; set; }

        public static void Main(string[] _)
        {

        }

        [SetupMethod]
        internal static void Setup(IApplicationBuilder builder)
        {
            builder.ConfigureContainer<IContainer>(map => InitMap = map);
        }
    }
}
//</assembly>

//<assembly:manual />
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
namespace SetupMethodManualInjectionTests
{
    using System;

    using AutoDI;

    public class TestClass
    {
        public static IContainer? InitMap { get; set; }

        [SetupMethod]
        internal static void InitializeContainer(IApplicationBuilder builder)
        {
            builder.ConfigureContainer<IContainer>(map => InitMap = map);
        }
    }

    public interface IManager { }

    public class Manager : IManager
    {
        public IService Service { get; }
        public Manager([Dependency] IService service = null!)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>