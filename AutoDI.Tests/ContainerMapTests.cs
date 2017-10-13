using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

            map.TypeKeyNotFoundEvent += delegate (object sender, TypeKeyNotFoundEventArgs e)
            {
                eventRaised = true;
            };

            services.AddAutoDISingleton<IInterface, Class>();
            map.Add(services);

            var c = map.Get("I'm not your type".GetType(), null);
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void TestTypeKeyNotFoundEventIsNotRaised()
        {
            var map = new ContainerMap();
            var services = new AutoDIServiceCollection();
            bool eventRaised = false;

            map.TypeKeyNotFoundEvent += delegate (object sender, TypeKeyNotFoundEventArgs e)
            {
                eventRaised = true;
            };

            services.AddAutoDISingleton<IInterface, Class>();
            map.Add(services);

            var c = map.Get<IInterface>(null);
            Assert.IsFalse(eventRaised);
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

        private interface IInterface { }

        private interface IInterface2 { }

        public class Class : IInterface, IInterface2 { }

        public class Derived : Class { }
    }
}
