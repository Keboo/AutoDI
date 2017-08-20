using System;
using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;
using AutoDI.Fody.Tests.PropertyInjectionNamespace;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class PropertyInjectionTests
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
            DI.Dispose();
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
            catch (Exception e) when(e.Is<MyException>(GetType()))
            {
                // expected exception
            }
        }
    }
    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI />
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
            [Dependency]
            public IService Service { get; set; }
        }

        public class ReadOnlyProperty
        {
            [Dependency]
            public IService Service { get; }
        }

        public class PrivateProperty
        {
            [Dependency]
            private IService Service { get; }

            public IService GetService() => Service;
        }

        public class PropertyWithImplementation
        {
            private IService _service;
        
            [Dependency]
            public IService Service
            {
                get
                {
                    if (_service != null)
                    {
                        return _service;
                    }
                    return null;
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
