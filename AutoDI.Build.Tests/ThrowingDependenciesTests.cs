using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using MethodDependencyNamespace;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ThrowingDependencyNamespace;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class ThrowingDependenciesTests
    {
        private static Assembly _testAssembly = null!;
        private bool _initialized;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            Generator gen = new();

            _testAssembly = (await gen.Execute()).SingleAssembly();
        }

        [TestInitialize]
        public void TestSetup()
        {
            DI.Init(_testAssembly);
            _initialized = true;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (_initialized)
            {
                DI.Dispose(_testAssembly);
            }
        }

        [TestMethod]
        [Description("Issue 137")]
        public void DependencyWithThrowingConstructorPropogatesException()
        {
            Exception ex = Assert.ThrowsException<Exception>(() =>
            {
                dynamic tester = _testAssembly.CreateInstance<Tester>();
                tester.InstanceThrow();
            });
            
            Assert.AreEqual("Constructor exception", ex.Message);
        }

        [TestMethod]
        [Description("Issue 137")]
        public void DependencyWithThrowingStaticConstructorPropogatesException()
        {
            TypeInitializationException ex = Assert.ThrowsException<TypeInitializationException>(() =>
            {
                dynamic tester = _testAssembly.CreateInstance<Tester>();
                tester.StaticThrow();
            });

            Assert.AreEqual("Static constructor exception", ex.InnerException.Message);
        }
    }
}

//<assembly>
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
namespace ThrowingDependencyNamespace
{
    using AutoDI;

    using System;

    public class Tester
    {
        public void InstanceThrow()
            => _ = new ClassWithThrowingDependency();

        public void StaticThrow()
            => _ = new ClassWithStaticThrowingDependency();
    }

    public class ClassWithThrowingDependency
    {
        public ClassWithThrowingDependency([Dependency] IThrowException service = null!)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }
        }
    }

    public class ClassWithStaticThrowingDependency
    {
        public ClassWithStaticThrowingDependency([Dependency] IThrowStaticException service = null!)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }
        }
    }

    public interface IThrowException { }

    public class ThrowException : IThrowException
    {
        public ThrowException()
        {
            throw new Exception("Constructor exception");
        }
    }

    public interface IThrowStaticException { }
    
    public class ThrowStaticException : IThrowStaticException
    {
        static ThrowStaticException()
        {
            throw new Exception("Static constructor exception");
        }
    }
}
//</assembly>