using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI.Tests
{
    [TestClass]
    public class ContainerMapTests
    {
        [TestMethod]
        public void TestTypeKeyNotFoundEventIsRaised()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            bool eventRaised = false;

            map.TypeKeyNotFound += (_, __) =>
            {
                eventRaised = true;
            };

            services.AddAutoDISingleton<IInterface, Class>();
            map.Add(services);

            map.Get<string>(null);
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void TestTypeKeyNotFoundEventIsNotRaised()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            bool eventRaised = false;

            map.TypeKeyNotFound += (_, __) =>
            {
                eventRaised = true;
            };

            services.AddAutoDISingleton<IInterface, Class>();
            map.Add(services);

            map.Get<IInterface>(null);
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public void TestTypeKeyNotFoundEventCanInsertType()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            var @class = new Class();

            map.TypeKeyNotFound += (_, e) =>
            {
                e.Instance = @class;
            };
            
            map.Add(services);

            IInterface retriecedService = map.Get<IInterface>(null);
            Assert.AreEqual(@class, retriecedService);
        }

        [TestMethod]
        public void TestGetSingleton()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface, Class>();
            map.Add(services);

            IInterface c = map.Get<IInterface>(null);
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetLazySingleton()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDILazySingleton<IInterface, Class>();
            map.Add(services);

            IInterface c = map.Get<IInterface>(null);
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetWeakSingleton()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDIWeakSingleton<IInterface, Class>();
            map.Add(services);

            IInterface c = map.Get<IInterface>(null);
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetTransient()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDITransient<IInterface, Class>();
            map.Add(services);

            IInterface c = map.Get<IInterface>(null);
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void GetSingletonAlwaysReturnsTheSameInstance()
        {
            var map = new ContainerMap();
            var instance = new Class();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface>(instance);
            map.Add(services);

            IInterface c1 = map.Get<IInterface>(null);
            IInterface c2 = map.Get<IInterface>(null);
            Assert.IsTrue(ReferenceEquals(c1, c2));
            Assert.IsTrue(ReferenceEquals(c1, instance));
            Assert.IsTrue(ReferenceEquals(c2, instance));
        }

        [TestMethod]
        public void GetLazySingletonDoesNotCreateObjectUntilRequested()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDILazySingleton<IInterface, Class>(provider => throw new Exception());
            map.Add(services);

            try
            {
                map.Get<IInterface>(null);
            }
            catch (Exception)
            {
                return;
            }
            Assert.Fail("Exception should have been thrown");
        }

        [TestMethod]
        public void GetLazySingletonReturnsTheSameInstance()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDIService<Class>(provider => new Class(), new[] {typeof(IInterface), typeof(IInterface2)}, Lifetime.LazySingleton);
            map.Add(services);

            var instance1 = map.Get<IInterface>(null);
            var instance2 = map.Get<IInterface2>(null);
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.IsTrue(ReferenceEquals(instance1, instance2));
        }

        [TestMethod]
        public void GetWeakSingletonOnlyCreatesOneInstanceAtATime()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            int instanceCount = 0;
            services.AddAutoDIService<Class>(provider =>
            {
                instanceCount++;
                return new Class();
            }, new[] {typeof(IInterface)}, Lifetime.WeakSingleton);
            map.Add(services);

            var instance = map.Get<IInterface>(null);

            Assert.IsTrue(ReferenceEquals(instance, map.Get<IInterface>(null)));
            Assert.IsTrue(ReferenceEquals(instance, map.Get<IInterface>(null)));

            Assert.AreEqual(1, instanceCount);

            // ReSharper disable once RedundantAssignment
            instance = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

            Assert.IsNotNull(map.Get<IInterface>(null));
            Assert.AreEqual(2, instanceCount);
        }

        [TestMethod]
        public void GetTransientAlwaysCreatesNewInstances()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            int instanceCount = 0;
            services.AddAutoDIService<Class>(provider =>
            {
                instanceCount++;
                return new Class();
            }, new[] { typeof(IInterface) }, Lifetime.Transient);
            map.Add(services);

            var a = map.Get<IInterface>(null);
            var b = map.Get<IInterface>(null);
            var c = map.Get<IInterface>(null);
            Assert.IsFalse(ReferenceEquals(a, b));
            Assert.IsFalse(ReferenceEquals(b, c));
            Assert.IsFalse(ReferenceEquals(a, c));
            Assert.AreEqual(3, instanceCount);
        }

        [TestMethod]
        [Description("Issue 22")]
        public void ContainerMapCanGenerateLazyInstances()
        {
            var map = new ContainerMap();
            var @class = new Class();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface>(@class);
            map.Add(services);

            Lazy<IInterface> lazy = map.Get<Lazy<IInterface>>(null);
            Assert.IsNotNull(lazy);
            Assert.IsTrue(ReferenceEquals(@class, lazy.Value));
        }

        [TestMethod]
        [Description("Issue 22")]
        public void ContainerMapCanGenerateFuncInstances()
        {
            var map = new ContainerMap();
            var @class = new Class();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface>(@class);
            map.Add(services);

            Func<IInterface> func = map.Get<Func<IInterface>>(null);
            Assert.IsNotNull(func);
            Assert.IsTrue(ReferenceEquals(@class, func()));
        }

        [TestMethod]
        [Description("Issue 106")]
        public void ContainerMapCanConstructTypeWithDefaultConstructor()
        {
            var map = new ContainerMap();

            Class @class = map.Get<Class>(null);
            Assert.IsInstanceOfType(@class, typeof(Class));
        }

        [TestMethod]
        [Description("Issue 106")]
        public void ContainerMapCanConstructTypeWhoseDependenciesAreMapped()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDITransient<IInterface, Class>();
            map.Add(services);

            ClassWtihParameters @class = map.Get<ClassWtihParameters>(null);
            Assert.IsInstanceOfType(@class, typeof(ClassWtihParameters));
            Assert.IsInstanceOfType(@class.Service, typeof(Class));
        }

        [TestMethod]
        [Description("Issue 106")]
        public void AutoConstructedTypesAlwaysReturnNewInstances()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDITransient<IInterface, Class>();
            map.Add(services);

            ClassWtihParameters class1 = map.Get<ClassWtihParameters>(null);
            Assert.IsInstanceOfType(class1, typeof(ClassWtihParameters));

            ClassWtihParameters class2 = map.Get<ClassWtihParameters>(null);
            Assert.IsInstanceOfType(class2, typeof(ClassWtihParameters));

            Assert.IsFalse(ReferenceEquals(class1, class2));
        }

        [TestMethod]
        [Description("Issue 124")]
        public void CanResolveClosedGenericFromOpenGenericRegistration()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
            map.Add(services);

            var logger1 = map.Get<ILogger<MyClass>>(null);
            var logger2 = map.Get<ILogger<MyOtherClass>>(null);

            Assert.IsNotNull(logger1);
            Assert.IsNotNull(logger2);
            Assert.IsTrue(ReferenceEquals(logger1, map.Get<ILogger<MyClass>>(null)));
            Assert.IsTrue(ReferenceEquals(logger2, map.Get<ILogger<MyOtherClass>>(null)));
        }

        [TestMethod]
        [Description("Issue 124")]
        public void CanResolveClosedGenericFromOpenGenericRegistrationWithParameter()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(LoggerEx<>)));
            services.Add(ServiceDescriptor.Singleton(typeof(ILoggerFactory), typeof(LoggerFactory)));
            map.Add(services);

            var logger = map.Get<ILogger<MyClass>>(null) as LoggerEx<MyClass>;

            Assert.IsNotNull(logger);
            Assert.IsTrue(logger.Factory is LoggerFactory);
        }

        [TestMethod]
        [Description("Issue 127")]
        public void CanResolveSingleIEnumerableContructoreParameter()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface, Derived1>();
            map.Add(services);

            var @class = map.Get<ClassWithParameter<IEnumerable<IInterface>>>(null);

            Assert.IsNotNull(@class);
            CollectionAssert.AreEquivalent(
                new[] {typeof(Derived1)}, 
                @class.Parameter.Select(x => x.GetType()).ToArray());
        }

        [TestMethod]
        [Description("Issue 127")]
        public void CanResolveMultipleIEnumerableContructoreParameter()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface, Derived1>();
            services.AddAutoDISingleton<IInterface, Derived2>();
            map.Add(services);

            var @class = map.Get<ClassWithParameter<IEnumerable<IInterface>>>(null);

            Assert.IsNotNull(@class);
            CollectionAssert.AreEquivalent(
                new[] {typeof(Derived1), typeof(Derived2)}, 
                @class.Parameter.Select(x => x.GetType()).ToArray());
        }

        [TestMethod]
        [Description("Issue 127")]
        public void CanResolveSingleArrayContructoreParameter()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface, Derived1>();
            services.AddAutoDISingleton<IInterface, Derived2>();
            map.Add(services);

            var @class = map.Get<ClassWithParameter<IInterface[]>>(null);

            Assert.IsNotNull(@class);
            CollectionAssert.AreEquivalent(
                new[] {typeof(Derived1), typeof(Derived2)}, 
                @class.Parameter.Select(x => x.GetType()).ToArray());
        }

        [TestMethod]
        [Description("Issue 127")]
        public void CanResolveMultipleArrayContructoreParameter()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            services.AddAutoDISingleton<IInterface, Derived1>();
            services.AddAutoDISingleton<IInterface, Derived2>();
            map.Add(services);

            var @class = map.Get<ClassWithParameter<IInterface[]>>(null);

            Assert.IsNotNull(@class);
            CollectionAssert.AreEquivalent(
                new[] {typeof(Derived1), typeof(Derived2)}, 
                @class.Parameter.Select(x => x.GetType()).ToArray());
        }

        private interface IInterface { }

        private interface IInterface2 { }

        private interface ILogger<T> { }

        private class Logger<T> : ILogger<T> { }

        private class LoggerEx<T> : ILogger<T>
        {
            public ILoggerFactory Factory { get; }

            public LoggerEx(ILoggerFactory factory)
            {
                Factory = factory;
            }
        }

        private interface ILoggerFactory { }

        private class LoggerFactory : ILoggerFactory { }

        private class MyClass { }
        private class MyOtherClass { }

        private class Class : IInterface, IInterface2 { }

        private class Derived : Class { }

        private class Derived1 : IInterface { }
        private class Derived2 : IInterface { }
        private class Derived3 : Derived2 { }

        private class ClassWithParameter<T>
        {
            public ClassWithParameter(T parameter)
            {
                Parameter = parameter;
            }

            public T Parameter { get; }
        }

        private class ClassWtihParameters
        {
            public  IInterface Service { get; }
            public ClassWtihParameters(IInterface service)
            {
                Service = service ?? throw new ArgumentNullException(nameof(service));
            }
        }
    }
}
