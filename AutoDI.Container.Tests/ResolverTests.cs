using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResolveTestsNamespace;
using System;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.Container.Fody;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class ResolverTests
    {
        private static Assembly _testAssembly;

        private static IDependencyResolver _resolver;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            //Add AutoDI reference
            gen.AddReference(typeof(DependencyAttribute).Assembly.Location);
            gen.AddWeaver("AutoDI");
            gen.AddWeaver("AutoDI.Container");

            _testAssembly = await gen.Execute();

            Type resolverType = _testAssembly.GetType("AutoDI.AutoDIContainer");
            Assert.IsNotNull(resolverType, "Could not find generated AudoDI resolver");
            _resolver = Activator.CreateInstance(resolverType) as IDependencyResolver;
            Assert.IsNotNull(_resolver, "Failed to create resolver");
        }

        private object Resolve<T>()
        {
            return _testAssembly.InvokeGeneric<T>(_resolver, nameof(_resolver.Resolve), (object)new object[0]);
        }

        [TestMethod]
        public void CanResolveSingleInterfaceImplementationsByInterface()
        {
            Assert.IsTrue(Resolve<IService>().Is<Service>());
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
            Assert.IsTrue(ReferenceEquals(@interface, @class));
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
            Assert.IsTrue(Resolve<Base2>().Is<Derived2>());
            string foo = AutoDIContainer.GetMap(_testAssembly).ToString();
        }

        [TestMethod]
        public void CannotResolveClassByBaseTypeIfThereAreMultipleDerivedClasses()
        {
            Assert.IsNull(Resolve<Base>());
        }
    }
}

//<gen>
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
//</gen>
