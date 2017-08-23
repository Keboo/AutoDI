using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class SingletonTests
    {
        private static Assembly _singleton;
        private static Assembly _inSetup;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            gen.WeaverAdded += (sender, args) =>
            {
                var xml = XElement.Parse(@"
                    <AutoDI>
                        <type name="".*"" lifetime=""Singleton"" />
                    </AutoDI>");
                dynamic weaver = args.Weaver;
                weaver.Config = xml;
            };

            var testAssemblies = await gen.Execute();
            _singleton = testAssemblies["singleton"].Assembly;
            _inSetup = testAssemblies["inSetup"].Assembly;
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

    //<assembly:singleton />
    //<ref: AutoDI/>
    //<weaver: AutoDI />
    namespace SingletonResolutionTest
    {
        using AutoDI;
        using System;

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
    //<weaver: AutoDI />
    namespace SingletonResolvedInSetupMethod
    {
        using AutoDI;
        using System;

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
                builder.ConfigureContinaer<ContainerMap>(map =>
                {
                    var service = map.Get<IService>(null);
                    if (!(service is Service)) throw new Exception();
                });
            }
        }
    }
    //</assembly>
}