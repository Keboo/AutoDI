using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class SetupMethodTests
    {
        private static Assembly _publicAssembly;
        private static Assembly _internalAssembly;
        private static Assembly _manualAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            Dictionary<string, AssemblyInfo> result = await gen.Execute();
            _publicAssembly = result["public"].Assembly;
            _internalAssembly = result["internal"].Assembly;
            _manualAssembly = result["manual"].Assembly;
        }

        [TestMethod]
        [Description("Issue 23")]
        public void PublicSetupMethodIsInvoked()
        {
            ContainerMap GetInitMap()
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
            ContainerMap GetInitMap()
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
            ContainerMap GetInitMap()
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
        public static IContainer InitMap { get; set; }

        public static void Main(string[] args)
        {
            
        }

        [SetupMethod]
        public static void Setup(IApplicationBuilder builder)
        {
            builder.ConfigureContainer<IContainer>(map =>
            {
                InitMap = map;
            });
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
        public static IContainer InitMap { get; set; }

        public static void Main(string[] args)
        {

        }

        [SetupMethod]
        internal static void Setup(IApplicationBuilder builder)
        {
            builder.ConfigureContainer<IContainer>(map =>
            {
                InitMap = map;
            });
        }
    }
}
//</assembly>

//<assembly:manual />
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
namespace SetupMethodManualInjectionTests
{
    using AutoDI;
    using System;

    public class TestClass
    {
        public static IContainer InitMap { get; set; }

        [SetupMethod]
        internal static void InitializeContainer(IApplicationBuilder builder)
        {
            builder.ConfigureContainer<IContainer>(map =>
            {
                InitMap = map;
            });
        }
    }

    public interface IManager { }

    public class Manager : IManager
    {
        public IService Service { get; }
        public Manager([Dependency] IService service = null)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>