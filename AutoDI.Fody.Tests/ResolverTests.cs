using AutoDI.AssemblyGenerator;
using AutoDI.Build.Tests.ResolveTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class ResolverTests
    {
        private static Assembly _testAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();

            DI.Init(_testAssembly);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose(_testAssembly);
        }

        private object Resolve<T>()
        {
            string assemblyTypeName = TypeMixins.GetTypeName(typeof(T), GetType());
            Type resolveType = _testAssembly.GetType(assemblyTypeName);

            IServiceProvider provider = DI.GetGlobalServiceProvider(_testAssembly);
            return provider.GetService(resolveType, new object[0]);
        }

        [TestMethod]
        public void CanResolveSingleInterfaceImplementationsByInterface()
        {
            Assert.IsTrue(Resolve<IService>().Is<Service>(GetType()));
        }

        [TestMethod]
        public void CanResolveSingleInterfaceImplementationsByClass()
        {
            Assert.IsNotNull(Resolve<Service>());
        }

        [TestMethod]
        public void CanResolveSingletonInstance()
        {
            // ReSharper disable once EqualExpressionComparison
            Assert.IsTrue(ReferenceEquals(Resolve<IService>(), Resolve<IService>()));
        }

        [TestMethod]
        public void CanResolveSingletonByClassOrInterface()
        {
            var @interface = Resolve<IService>();
            var @class = Resolve<Service>();
            Assert.IsNotNull(@interface);
            Assert.IsNotNull(@class);
            //NB: The class registration is transient while the single interfance registration is lazy singleton.
            //These are different registrations.
            Assert.IsFalse(ReferenceEquals(@interface, @class));
        }

        [TestMethod]
        public void CanResolveSingletonByAnyInterface()
        {
            var interface1 = Resolve<IService>();
            var interface2 = Resolve<IService2>();
            Assert.IsNotNull(interface1);
            Assert.IsNotNull(interface2);
            Assert.IsTrue(ReferenceEquals(interface1, interface2));
        }

        [TestMethod]
        public void CanResolveClassByBaseType()
        {
            //This works because it is the only derived class from Base2
            Assert.IsTrue(Resolve<Base2>().Is<Derived2>(GetType()));
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace ResolveTestsNamespace
    {
        public interface IService
        { }

        public interface IService2
        { }

        public class Service : IService, IService2
        { }

        public abstract class Base
        { }

        public abstract class Base2 : Base
        { }

        public class Derived1 : Base
        { }

        public class Derived2 : Base2
        { }
    }
    //</assembly>
}
