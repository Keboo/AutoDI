using AutoDI.AssemblyGenerator;
using AutoDI.Build.Tests.ScopeTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI.Build.Tests
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
                    /*args.Weaver.Instance.Config = XElement.Parse($@"
    <AutoDI>
        <type name=""*"" Lifetime=""{Lifetime.Scoped}"" />
    </AutoDI>");*/
                }
            };

            _testAssembly = (await gen.Execute()).SingleAssembly();

            DI.Init(_testAssembly, app => app.ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddAutoDIScoped(ResolveType(typeof(ILogger<>)), ResolveType(typeof(Logger<>)));
                }));
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose(_testAssembly);
        }

        private static Type ResolveType(Type type)
        {
            string assemblyTypeName = TypeMixins.GetTypeName(type, typeof(ScopeTests));
            return _testAssembly.GetType(assemblyTypeName);
        }

        private object Resolve<T>(IServiceScope scope = null)
        {
            Type resolveType = ResolveType(typeof(T));
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

        [TestMethod]
        [Description("Issue 124")]

        public void CanResolveClosedGenericFromOpenGenericRegistrationInScope()
        {
            var scopeFactory = DI.GetGlobalServiceProvider(_testAssembly).GetService<IServiceScopeFactory>();

            using (IServiceScope scope1 = scopeFactory.CreateScope())
            using (IServiceScope scope2 = scopeFactory.CreateScope())
            {
                var logger1A = Resolve<ILogger<MyClass>>(scope1);
                var logger1B = Resolve<ILogger<MyOtherClass>>(scope1);

                Assert.IsNotNull(logger1A);
                Assert.IsNotNull(logger1B);
                Assert.IsTrue(ReferenceEquals(logger1A, Resolve<ILogger<MyClass>>(scope1)));
                Assert.IsTrue(ReferenceEquals(logger1B, Resolve<ILogger<MyOtherClass>>(scope1)));

                var logger2A = Resolve<ILogger<MyClass>>(scope2);
                var logger2B = Resolve<ILogger<MyOtherClass>>(scope2);

                Assert.IsNotNull(logger2A);
                Assert.IsNotNull(logger2B);
                Assert.IsTrue(ReferenceEquals(logger2A, Resolve<ILogger<MyClass>>(scope2)));
                Assert.IsTrue(ReferenceEquals(logger2B, Resolve<ILogger<MyOtherClass>>(scope2)));

                Assert.IsFalse(ReferenceEquals(logger1A, logger2A));
                Assert.IsFalse(ReferenceEquals(logger1B, logger2B));
            }
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

        public interface ILogger<T> { }

        public class Logger<T> : ILogger<T> { }

        public class MyClass { }
        public class MyOtherClass { }
    }
    //</assembly>
}
