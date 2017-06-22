using System;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResolveTestsNamespace;

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
            Assert.IsTrue(ReferenceEquals(_resolver.Resolve<IService>(), _resolver.Resolve<IService>()));
        }

        [TestMethod]
        public void CanResolveSingletonByClassOrInterface()
        {
            Assert.IsTrue(ReferenceEquals(_resolver.Resolve<Service>(), _resolver.Resolve<IService>()));
        }

        [TestMethod]
        public void CanResolveSingletonByAnyInterface()
        {
            Assert.IsTrue(ReferenceEquals(_resolver.Resolve<IService>(), _resolver.Resolve<IService2>()));
        }
    }
}

//<gen>
namespace ResolveTestsNamespace
{
    using AutoDI;
    using System;

    public interface IService
    { }

    public interface IService2
    { }

    public class Service : IService, IService2
    { }
}
//</gen>
