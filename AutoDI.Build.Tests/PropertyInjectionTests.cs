using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;
using AutoDI.Build.Tests.PropertyInjectionNamespace;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class PropertyInjectionTests
    {
        private static Assembly _testAssembly = null!;
        private static bool _initialized;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();
            DI.Init(_testAssembly);
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
        public void CanSetSimpleProperty()
        {
            dynamic @class = _testAssembly.CreateInstance<SimpleProperty>(GetType());
            object service = @class.Service;
            Assert.IsTrue(service.Is<Service>(GetType()));
        }


        [TestMethod]
        public void CanSetReadOnlyProperty()
        {
            dynamic @class = _testAssembly.Resolve<ReadOnlyProperty>(GetType());
            object service = @class.Service;
            Assert.IsTrue(service.Is<Service>(GetType()));
        }

        [TestMethod]
        public void CanSetPrivateProperty()
        {
            dynamic @class = _testAssembly.Resolve<PrivateProperty>(GetType());
            object service = @class.GetService();
            Assert.IsTrue(service.Is<Service>(GetType()));
        }

        [TestMethod]
        public void CanSetPropertyWithImplementation()
        {
            dynamic @class = _testAssembly.Resolve<PropertyWithImplementation>(GetType());
            object service = @class.Service;
            Assert.IsTrue(service.Is<Service>(GetType()));
        }

        [TestMethod]
        public void IgnoresPropertiesWithoutSettersOrBackingFields()
        {
            dynamic @class = _testAssembly.Resolve<PropertyWithoutSetter>(GetType());

            try
            {
                var service = @class.Service;
                Assert.Fail("Expected exception");
            }
            catch (Exception e) when (e.Is<MyException>(GetType()))
            {
                // expected exception
            }
        }
    }
    
    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace PropertyInjectionNamespace
    {
        using System;

        using AutoDI;

        public interface IService
        { }

        public class Service : IService
        { }

        public class SimpleProperty
        {
            private string _foo = "Bar";

            [Dependency]
            public IService Service { get; set; } = null!;

            public string Foo
            {
                get
                {
                    return _foo;
                }
                set => _foo = value;
            }

            public SimpleProperty()
            {
                Foo = "Foo";
            }
        }

        public class ReadOnlyProperty
        {
            [Dependency]
            public IService Service { get; } = null!;
        }

        public class PrivateProperty
        {
            [Dependency]
            private IService Service { get; } = null!;

            public IService GetService() => Service;
        }

        public class PropertyWithImplementation
        {
            private IService? _service;

            [Dependency]
            public IService? Service
            {
                get
                {
                    return _service ?? null;
                }
                set
                {
                    if (value != null)
                    {
                        _service = value;
                    }
                }
            }
        }

        public class PropertyWithoutSetter
        {
            [Dependency]
            public IService Service => throw new MyException();
        }

        public class MyException : Exception { }
    }
    //</assembly>
}