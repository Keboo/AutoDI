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
            map.AddSingleton(new Class(), new[] { typeof(IInterface) });

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetLazySingleton()
        {
            var map = new InternalMap();
            map.AddLazySingleton(() => new Class(), new[]{typeof(IInterface)});

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetWeakTransient()
        {
            var map = new InternalMap();
            map.AddWeakTransient(() => new Class(), new[] { typeof(IInterface) });

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetTransient()
        {
            var map = new InternalMap();
            map.AddTransient(() => new Class(), new[] { typeof(IInterface) });

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void GetSingletonAlwaysReturnsTheSameInstance()
        {
            var map = new InternalMap();
            var instance = new Class();
            map.AddSingleton(instance, new[] { typeof(IInterface) });

            IInterface c1 = map.Get<IInterface>();
            IInterface c2 = map.Get<IInterface>();
            Assert.IsTrue(ReferenceEquals(c1, c2));
            Assert.IsTrue(ReferenceEquals(c1, instance));
            Assert.IsTrue(ReferenceEquals(c2, instance));
        }

        [TestMethod]
        public void GetLazySingletonDoesNotCreateObjectUntilRequested()
        {
            var map = new InternalMap();
            map.AddLazySingleton<Class>(() => throw new Exception(), new[] { typeof(IInterface) });

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
            var map = new InternalMap();
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
            var map = new InternalMap();
            int instanceCount = 0;
            map.AddWeakTransient(() =>
            {
                instanceCount++;
                return new Class();
            }, new []{typeof(IInterface)});

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
            map.AddTransient(() =>
            {
                instanceCount++;
                return new Class();
            }, new[] {typeof(IInterface)});

            var a = map.Get<IInterface>();
            var b = map.Get<IInterface>();
            var c = map.Get<IInterface>();
            Assert.IsFalse(ReferenceEquals(a, b));
            Assert.IsFalse(ReferenceEquals(b, c));
            Assert.IsFalse(ReferenceEquals(a, c));
            Assert.AreEqual(3, instanceCount);
        }

        public class AutoDiContainer : IDependencyResolver
        {
            private static readonly InternalMap _map = new InternalMap();

            static AutoDiContainer()
            {
                _map.AddSingleton(new Class(), new[] {typeof(IInterface)});

                _map.AddLazySingleton(() => new Class(), new[] {typeof(IInterface), typeof(IInterface)});
            }


            T IDependencyResolver.Resolve<T>(params object[] parameters)
            {
                return _map.Get<T>();
            }
        }

        private interface IInterface { }

        private interface IInterface2 { }

        public class Class : IInterface, IInterface2 { }
    }
}
