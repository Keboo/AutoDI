using AutoDI.Container.Fody;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class MapTests
    {
        [TestMethod]
        public void TestGetSingleton()
        {
            var map = new InternalMap();
            map.AddSingleton<IInterface, Class>(new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetLazySingleton()
        {
            var map = new InternalMap();
            map.AddLazySingleton<IInterface, Class>(() => new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetWeakTransient()
        {
            var map = new InternalMap();
            map.AddWeakTransient<IInterface, Class>(() => new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetTransient()
        {
            var map = new InternalMap();
            map.AddTransient<IInterface, Class>(() => new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void GetOnceAlwaysReturnsTheSameInstance()
        {
            var map = new InternalMap();
            var instance = new Class();
            map.AddSingleton<IInterface, Class>(instance);

            IInterface c1 = map.Get<IInterface>();
            IInterface c2 = map.Get<IInterface>();
            Assert.IsTrue(ReferenceEquals(c1, c2));
            Assert.IsTrue(ReferenceEquals(c1, instance));
            Assert.IsTrue(ReferenceEquals(c2, instance));
        }

        [TestMethod]
        public void GetOnceLazyDoesNotCreateObjectUntilRequested()
        {
            var map = new InternalMap();
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
        public void GetSingleOnlyCreatesOneInstanceAtATime()
        {
            var map = new InternalMap();
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
            var map = new InternalMap();
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

        public class AutoDiContainer
        {
            private static readonly InternalMap _map = new InternalMap();

            static AutoDiContainer()
            {
                _map.AddSingleton<IInterface, Class>(new Class());

                _map.AddLazySingleton<IInterface, Class>(() => new Class());
            }
        }

        private interface IInterface
        { }

        public class Class : IInterface { }
    }
}
