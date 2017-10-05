using AutoDI.AssemblyGenerator;
using AutoDI.Fody.Tests.ScopeTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class ScopeTests
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
                    dynamic weaver = args.Weaver;
                    weaver.Config = XElement.Parse($@"
    <AutoDI>
        <type name="".*"" Lifetime=""{Lifetime.Scoped}"" />
    </AutoDI>");
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();

            DI.Init(_testAssembly);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose(_testAssembly);
        }

        private object Resolve<T>(IServiceScope scope = null)
        {
            string assemblyTypeName = TypeMixins.GetTypeName(typeof(T), GetType());
            Type resolveType = _testAssembly.GetType(assemblyTypeName);
            return (scope?.ServiceProvider ?? DI.GetGlobalServiceProvider(_testAssembly)).GetService(resolveType, new object[0]);
        }

        [TestMethod]
        public void CanResolveScopedSingletonsByInterface()
        {
            var scopeFactory = DI.GetGlobalServiceProvider(_testAssembly).GetService<IServiceScopeFactory>();

            using (IServiceScope scope1 = scopeFactory.CreateScope())
            using (IServiceScope scope2 = scopeFactory.CreateScope())
            {
                var service1A = Resolve<IService>(scope1);
                var service1B = Resolve<IService>(scope1);
                var service2A = Resolve<IService>(scope2);
                var service2B = Resolve<IService>(scope2);

                Assert.IsNotNull(service1A);
                Assert.IsNotNull(service1B);
                Assert.IsNotNull(service2A);
                Assert.IsNotNull(service2B);
                Assert.IsTrue(ReferenceEquals(service1A, service1B));
                Assert.IsTrue(ReferenceEquals(service2A, service2B));
                Assert.IsFalse(ReferenceEquals(service1A, service2A));
            }

            Assert.IsTrue(Resolve<IService>().Is<Service>(GetType()));
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace ScopeTestsNamespace
    {
        public interface IService
        { }

        public interface IService2
        { }

        public class Service : IService, IService2
        { }
    }
    //</assembly>
}
