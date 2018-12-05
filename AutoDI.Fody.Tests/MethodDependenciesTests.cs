using AutoDI.AssemblyGenerator;
using MethodDependencyNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class MethodDependenciesTests
    {
        private static Assembly _testAssembly;
        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestInitialize]
        public void TestSetup()
        {
            DI.Init(_testAssembly);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DI.Dispose(_testAssembly);
        }

        [TestMethod]
        public void PublicMethodDependenciesAreInjected()
        {
            dynamic @class = _testAssembly.CreateInstance<ClassWithPublicMethodDependency>();
            @class.DoSomething();
            Assert.IsTrue(((object)@class.Service).Is<Service>());
        }

        [TestMethod]
        public void PrivateMethodDependenciesAreInjected()
        {
            dynamic @class = _testAssembly.CreateInstance<ClassWithPrivateMethodDependency>();
            @class.DoSomething();
            Assert.IsTrue(((object)@class.Service).Is<Service>());
        }

        [TestMethod]
        public void ExplicitInterfaceMethodDependenciesAreInjected()
        {
            dynamic @class = _testAssembly.CreateInstance<ClassWithExplicitInterfaceImplementation>();
            @class.DoSomething();
            Assert.IsTrue(((object)((dynamic)@class).Service).Is<Service>());
        }
    }
}

//<assembly>
//<ref: AutoDI />
//<weaver: AutoDI />
namespace MethodDependencyNamespace
{
    using AutoDI;

    public class ClassWithPublicMethodDependency
    {
        public IService Service { get; private set; }

        public void DoSomething([Dependency] IService service = null)
        {
            Service = service;
        }
    }

    public class ClassWithPrivateMethodDependency
    {
        public IService Service { get; private set; }

        public void DoSomething() => DoSomethingPrivate();

        private void DoSomethingPrivate([Dependency] IService service = null)
        {
            Service = service;
        }
    }

    public class ClassWithExplicitInterfaceImplementation : IOther
    {
        public IService Service { get; private set; }

        public void DoSomething() => ((IOther) this).DoSomething();

        void IOther.DoSomething([Dependency]IService service)
        {
            Service = service;
        }
    }

    public interface IOther
    {
        void DoSomething(IService service = null);
    }

    public interface IService { }

    public class Service : IService { }
}
//</assembly>
