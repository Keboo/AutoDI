﻿using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;
using AutoDI.Build.Tests.ResolutionOrderingTestsNamespace;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class ResolutionOrderingTests
    {
        private static Assembly _testAssembly = null!;
        private static bool _initialized;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            var gen = new Generator();
            
            _testAssembly = (await gen.Execute()).SingleAssembly();
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
        public void ClassesShouldDefaultToResolvingToThemselves()
        {
            DI.Init(_testAssembly);

            var libraryClass = _testAssembly.Resolve<LibraryClass>(GetType());
            Assert.IsTrue(libraryClass.Is<LibraryClass>(GetType()), $"Expected {nameof(LibraryClass)} but was {libraryClass?.GetType().Name ?? "<null>"}");
            var myBaseClass = _testAssembly.Resolve<MyBaseClass>(GetType());
            Assert.IsTrue(myBaseClass.Is<MyBaseClass>(GetType()), $"Expected {nameof(MyBaseClass)} but was {myBaseClass?.GetType().Name ?? "<null>"}");
            var myClass = _testAssembly.Resolve<MyClass>(GetType());
            Assert.IsTrue(myClass.Is<MyClass>(GetType()), $"Expected {nameof(MyClass)} but was {myClass?.GetType().Name ?? "<null>"}");
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<ref: Microsoft.Extensions.DependencyInjection.Abstractions />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace ResolutionOrderingTestsNamespace
    {
        public class LibraryClass { }

        public class MyBaseClass : LibraryClass { }

        public class MyClass : MyBaseClass { }
    }
    //</assembly>
}