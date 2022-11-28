using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;


//<assembly:singleton />
//<ref: AutoDI/>
//<weaver: AutoDI.Build.ProcessAssemblyTask />
[assembly: AutoDI.Map(".*", AutoDI.Lifetime.Singleton)]
namespace SingletonResolutionTest
{
    using System;

    using AutoDI;

    public interface IService { }

    public class Service : IService
    {
        public static bool IsCreated { get; set; }

        public Service()
        {
            IsCreated = true;
        }
    }

    public static class Foo
    {
        [SetupMethod]
        public static void Setup(IApplicationBuilder builder)
        {
            if (Service.IsCreated) throw new Exception();
        }
    }
}
//</assembly>

//<assembly:inSetup />
//<ref: AutoDI/>
//<weaver: AutoDI.Build.ProcessAssemblyTask />
namespace SingletonResolvedInSetupMethod
{
    using System;

    using AutoDI;

    public interface IService { }

    public class Service : IService
    {
        public static bool IsCreated { get; set; }

        public Service()
        {
            IsCreated = true;
        }
    }

    public static class Foo
    {
        [SetupMethod]
        public static void Setup(IApplicationBuilder builder)
        {
            if (Service.IsCreated) throw new Exception();
            builder.ConfigureContainer<IContainer>(map =>
            {
                var service = map.Get<IService>(null);
                if (service is not Service) throw new Exception();
            });
        }
    }
}
//</assembly>

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class SingletonTests
    {
        private static Assembly _singleton = null!;
        private static Assembly _inSetup = null!;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            Generator gen = new();

            var testAssemblies = await gen.Execute();
            _singleton = testAssemblies["singleton"].Assembly ?? throw new Exception("Could not find singleton assembly");
            _inSetup = testAssemblies["inSetup"].Assembly ?? throw new Exception("Could not find inSetup assembly");
        }

        [TestMethod]
        [Description("Issue 55")]
        public void SingletonNotCreatedUntilAfterSetupMethod()
        {
            bool isCreated = (bool)_singleton.GetStaticProperty<SingletonResolutionTest.Service>(nameof(SingletonResolutionTest.Service
                .IsCreated), GetType());
            Assert.IsFalse(isCreated);

            DI.Init(_singleton);

            isCreated = (bool)_singleton.GetStaticProperty<SingletonResolutionTest.Service>(nameof(SingletonResolutionTest.Service
                .IsCreated), GetType());
            Assert.IsTrue(isCreated);
        }

        [TestMethod]
        [Description("Issue 55")]
        public void CanResolveGeneratedSingletonInsideOfSetupMethod()
        {
            bool isCreated = (bool)_inSetup.GetStaticProperty<SingletonResolvedInSetupMethod.Service>(nameof(SingletonResolvedInSetupMethod.Service
                .IsCreated), GetType());
            Assert.IsFalse(isCreated);

            DI.Init(_inSetup);
        }
    }
}