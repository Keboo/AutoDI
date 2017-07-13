using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class ContainerMapTests
    {
        [TestMethod]
        public void TestGetSingleton()
        {
            var map = new ContainerMap();
            map.AddSingleton<IInterface, Class>();

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetLazySingleton()
        {
            var map = new ContainerMap();
            map.AddLazySingleton<IInterface, Class>();

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetWeakTransient()
        {
            var map = new ContainerMap();
            map.AddWeakTransient<IInterface, Class>();

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetTransient()
        {
            var map = new ContainerMap();
            map.AddTransient<IInterface, Class>();

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void GetSingletonAlwaysReturnsTheSameInstance()
        {
            var map = new ContainerMap();
            var instance = new Class();
            map.AddSingleton<IInterface, Class>(instance);

            IInterface c1 = map.Get<IInterface>();
            IInterface c2 = map.Get<IInterface>();
            Assert.IsTrue(ReferenceEquals(c1, c2));
            Assert.IsTrue(ReferenceEquals(c1, instance));
            Assert.IsTrue(ReferenceEquals(c2, instance));
        }

        [TestMethod]
        public void GetLazySingletonDoesNotCreateObjectUntilRequested()
        {
            var map = new ContainerMap();
            map.AddLazySingleton<IInterface, Class>(() => throw new Exception());

            try
            {
                map.Get<IInterface>();
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
            map.AddLazySingleton(() => new Class(), new[] { typeof(IInterface), typeof(IInterface2) });

            var instance1 = map.Get<IInterface>();
            var instance2 = map.Get<IInterface2>();
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.IsTrue(ReferenceEquals(instance1, instance2));
        }

        [TestMethod]
        public void GetSingleOnlyCreatesOneInstanceAtATime()
        {
            var map = new ContainerMap();
            int instanceCount = 0;
            map.AddWeakTransient<IInterface, Class>(() =>
            {
                instanceCount++;
                return new Class();
            });

            var instance = map.Get<IInterface>();

            Assert.IsTrue(ReferenceEquals(instance, map.Get<IInterface>()));
            Assert.IsTrue(ReferenceEquals(instance, map.Get<IInterface>()));

            Assert.AreEqual(1, instanceCount);

            // ReSharper disable once RedundantAssignment
            instance = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

            Assert.IsNotNull(map.Get<IInterface>());
            Assert.AreEqual(2, instanceCount);
        }

        [TestMethod]
        public void GetAlwaysCreatesNewInstances()
        {
            var map = new ContainerMap();
            int instanceCount = 0;
            map.AddTransient<IInterface, Class>(() =>
            {
                instanceCount++;
                return new Class();
            });

            var a = map.Get<IInterface>();
            var b = map.Get<IInterface>();
            var c = map.Get<IInterface>();
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
            map.AddSingleton<IInterface, Class>(@class);

            Lazy<IInterface> lazy = map.Get<Lazy<IInterface>>();
            Assert.IsNotNull(lazy);
            Assert.IsTrue(ReferenceEquals(@class, lazy.Value));
        }

        [TestMethod]
        [Description("Issue 22")]
        public void ContainerMapCanGenerateFuncInstances()
        {
            var map = new ContainerMap();
            var @class = new Class();
            map.AddSingleton<IInterface, Class>(@class);

            Func<IInterface> func = map.Get<Func<IInterface>>();
            Assert.IsNotNull(func);
            Assert.IsTrue(ReferenceEquals(@class, func()));
        }

        [TestMethod]
        [Description("Issue 22")]
        public void CanRemoveMappedType()
        {
            var map = new ContainerMap();
            map.AddSingleton<IInterface, Class>();
            map.AddSingleton<IInterface2, Derived>();

            Assert.IsTrue(map.Remove<Class>());

            Assert.IsNull(map.Get<IInterface>());
            Assert.IsTrue(map.Get<IInterface2>() is Derived);
        }

        [TestMethod]
        [Description("Issue 22")]
        public void CanRemoveMappedTypeKeys()
        {
            var map = new ContainerMap();

            map.AddSingleton(new Class(), new[] {typeof(IInterface), typeof(IInterface2)});

            Assert.IsTrue(map.RemoveKey(typeof(IInterface)));

            Assert.IsNull(map.Get<IInterface>());
            Assert.IsTrue(map.Get<IInterface2>() is Class);
        }

        private interface IInterface { }

        private interface IInterface2 { }

        public class Class : IInterface, IInterface2 { }

        public class Derived : Class { }
    }
}
