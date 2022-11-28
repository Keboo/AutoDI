﻿using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    using CanResolveFromNonGenericNamespace;

    [TestClass]
    public class CanResolveFromNonGenericTests
    {
        private static Assembly _testAssembly = null!;
        private static bool _initialized;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            var gen = new Generator();
            _testAssembly = (await gen.Execute()).SingleAssembly();

            _testAssembly.InvokeEntryPoint();
            _initialized = true;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (_initialized)
            {
                DI.Dispose(_testAssembly);
            }
        }

        [TestMethod]
        [Description("Issue 26")]
        public void CanResolveServiceWithNonGenericMethod()
        {
            IServiceProvider provider = DI.GetGlobalServiceProvider(_testAssembly);
            Type serviceType = _testAssembly.GetType(TypeMixins.GetTypeName(typeof(IService), GetType()));

            Assert.IsTrue(provider.GetService(serviceType).Is<Service>(GetType()));
        }

        [TestMethod]
        [Description("Issue 26")]
        public void CanResolveServiceWithGenericMethod()
        {
            IServiceProvider provider = DI.GetGlobalServiceProvider(_testAssembly);

            Type serviceType = _testAssembly.GetType(TypeMixins.GetTypeName(typeof(IService), GetType()));

            var method = typeof(ServiceProviderServiceExtensions)
                .GetMethod(nameof(ServiceProviderServiceExtensions.GetService))
                ?.MakeGenericMethod(serviceType);
            Assert.IsTrue(method != null && method.Invoke(null, new object[] { provider }).Is<Service>(GetType()));
        }
    }

    //<assembly />
    //<type:ConsoleApplication/>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace CanResolveFromNonGenericNamespace
    {
        public class Program
        {
            public static void Main(string[] args)
            { }
        }

        public interface IService { }

        public class Service : IService { }
    }
    //</assembly>
}