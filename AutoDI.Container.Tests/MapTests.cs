using System;
using System.Threading.Tasks;
using AutoDI.Container.Fody;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Container.Tests
{
    [TestClass]
    public class MapTests
    {
        [TestMethod]
        public void TestGetFromOnce()
        {
            var map = new InternalMap();
            map.AddOnce<IInterface, Class>(new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetFromOnceLazy()
        {
            var map = new InternalMap();
            map.AddLazy<IInterface, Class>(() => new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetFromSingle()
        {
            var map = new InternalMap();
            map.AddSingle<IInterface, Class>(() => new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void TestGetFromAlways()
        {
            var map = new InternalMap();
            map.AddAlways<IInterface, Class>(() => new Class());

            IInterface c = map.Get<IInterface>();
            Assert.IsTrue(c is Class);
        }

        [TestMethod]
        public void GetOnceAlwaysReturnsTheSameInstance()
        {
            var map = new InternalMap();
            var instance = new Class();
            map.AddOnce<IInterface, Class>(instance);

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
            map.AddLazy<IInterface, Class>(() => throw new Exception());

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
            map.AddSingle<IInterface, Class>(() =>
            {
                instanceCount++;
                return new Class();
            });

            var instance = map.Get<IInterface>();

            Assert.IsTrue(ReferenceEquals(instance, map.Get<IInterface>()));

            var weakRef = new WeakReference<IInterface>(instance);
            instance = null;
            GC.Collect();
            while (weakRef.TryGetTarget(out instance))
            {
                instance = null;
                Task.Delay(TimeSpan.FromSeconds(1));
            }

            map.Get<IInterface>();
            Assert.AreEqual(2, instanceCount);
        }

        [TestMethod]
        public void GetAlwaysCreatesNewInstances()
        {
            var map = new InternalMap();
            int instanceCount = 0;
            map.AddAlways<IInterface, Class>(() =>
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

        private interface IInterface
        { }

        public class Class : IInterface { }
    }
}
