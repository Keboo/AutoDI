using System.Reflection;
using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

//<assembly>
//<ref: AutoDI />
//<weaver: AutoDI.Build.ProcessAssemblyTask />
[assembly: AutoDI.Map("Service+IService1", "Service+MyService2")]

namespace NestedClassesTestsNamespace
{
    public class Service
    {
        private class PrivateNested
        {
            private class PrivateNestedNested
            { }

            protected class ProtectedNestedNested
            { }

            internal class InternalNestedNested
            { }

            protected internal class ProtectedInternalNestedNested
            { }

            public class PublicNestedNested
            { }
        }

        protected class ProtectedNested
        {
            private class PrivateNestedNested
            { }

            protected class ProtectedNestedNested
            { }

            internal class InternalNestedNested
            { }

            protected internal class ProtectedInternalNestedNested
            { }

            public class PublicNestedNested
            { }
        }

        internal class InternalNested
        {
            private class PrivateNestedNested
            { }

            protected class ProtectedNestedNested
            { }

            internal class InternalNestedNested
            { }

            protected internal class ProtectedInternalNestedNested
            { }

            public class PublicNestedNested
            { }
        }

        protected internal class ProtectedInternalNested
        {
            private class PrivateNestedNested
            { }

            protected class ProtectedNestedNested
            { }

            internal class InternalNestedNested
            { }

            protected internal class ProtectedInternalNestedNested
            { }

            public class PublicNestedNested
            { }
        }

        public class PublicNested
        {
            private class PrivateNestedNested
            { }

            protected class ProtectedNestedNested
            { }

            internal class InternalNestedNested
            { }

            protected internal class ProtectedInternalNestedNested
            { }

            public class PublicNestedNested
            { }
        }

        public interface IService1 { }
        public class MyService1 : IService1 { }
        public class MyService2 : IService1 { }
    }
}
//</assembly>

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class NestedClassTests
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
            DI.Dispose(_testAssembly);
        }

        [TestMethod]
        [Description("Issue 75")]
        public void PrivateNestedClassIsNotMapped()
        {
            var provider = DI.GetGlobalServiceProvider(_testAssembly);
            ContainerMap containerMap = (ContainerMap)provider.GetService<IContainer>(Array.Empty<object>());

            foreach (var mappedType in containerMap
                .Select(m => m.TargetType)
                .Where(t => t.FullName?.StartsWith(nameof(NestedClassesTestsNamespace)) == true &&
                            t.Name.Contains("Nested")))
            {
                if (mappedType.Name.Contains("Private"))
                {
                    Assert.Fail($"Should not have mapped private type '{mappedType.FullName}'");
                }
                if (mappedType.Name.Contains("Protected") && !mappedType.Name.Contains("ProtectedInternal"))
                {
                    Assert.Fail($"Should not have mapped protected type '{mappedType.FullName}'");
                }
            }
        }

        [TestMethod]
        [Description("Issue 75")]
        public void PublicNestedClassIsMapped()
        {
            var provider = DI.GetGlobalServiceProvider(_testAssembly);
            ContainerMap containerMap = (ContainerMap)provider.GetService<IContainer>(Array.Empty<object>());

            //1 for the first nested class, 3 for each of the accessible sub nested classes
            Assert.AreEqual(4, containerMap.Count(m => m.TargetType.Name.StartsWith("PublicNested")));
        }

        [TestMethod]
        [Description("Issue 75")]
        public void ProtectedInternalNestedClassIsMapped()
        {
            var provider = DI.GetGlobalServiceProvider(_testAssembly);
            ContainerMap containerMap = (ContainerMap)provider.GetService<IContainer>(Array.Empty<object>());

            //1 for the first nested class, 3 for each of the accessible sub nested classes
            Assert.AreEqual(4, containerMap.Count(m => m.TargetType.Name.StartsWith("ProtectedInternalNested")));
        }

        [TestMethod]
        [Description("Issue 75")]
        public void InternalNestedClassIsMapped()
        {
            var provider = DI.GetGlobalServiceProvider(_testAssembly);
            ContainerMap containerMap = (ContainerMap)provider.GetService<IContainer>(Array.Empty<object>());

            //1 for the first nested class, 3 for each of the accessible sub nested classes
            Assert.AreEqual(4, containerMap.Count(m => m.TargetType.Name.StartsWith("InternalNested")));
        }

        [TestMethod]
        [Description("Issue 59")]
        public void CanMapNestedTypeWithPlus()
        {
            var provider = DI.GetGlobalServiceProvider(_testAssembly);
            ContainerMap containerMap = (ContainerMap)provider.GetService<IContainer>(Array.Empty<object>());

            var mappings = containerMap.ToList();
            Assert.IsTrue(mappings.Any(m => m.SourceType.Name == "IService1" && m.TargetType.Name == "MyService2"));
        }
    }
}