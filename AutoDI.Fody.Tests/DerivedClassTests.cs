using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoDI.AssemblyGenerator;
using DerivedClassTestsNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class DerivedClassTests
    {
        private static Assembly _testAssembly;
        private static IContainer _map;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testAssembly = (await gen.Execute()).SingleAssembly();

            DI.Init(_testAssembly, builder =>
            {
                builder.ConfigureContainer<IContainer>(map =>
                {
                    _map = map;
                });
            });
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            DI.Dispose(_testAssembly);
        }

        [TestMethod]
        [Description("Issue 121")]
        public void CanExcludeDerivedClasses()
        {
            //TODO: Outstanding question of how to handle this...
            var libraryClass = _testAssembly.Resolve<LibraryClass>();
            Assert.AreEqual(nameof(LibraryClass), libraryClass?.GetType().Name);
            var baseClass = _testAssembly.Resolve<MyBaseClass>();
            Assert.AreEqual(nameof(MyBaseClass), baseClass?.GetType().Name);
            var myClass = _testAssembly.Resolve<MyClass>();
            Assert.AreEqual(nameof(MyClass), myClass?.GetType().Name);

            var other = _testAssembly.Resolve<OtherBase>();
            Assert.AreEqual(nameof(AllYourBase), other?.GetType().Name);
        }
    }
}

//<assembly />
//<ref: AutoDI />
//<weaver: AutoDI />
namespace DerivedClassTestsNamespace
{
    public class LibraryClass { }

    public class MyBaseClass : LibraryClass { }

    public class MyClass : MyBaseClass { }

    public class OtherBase { }

    public class AllYourBase : OtherBase { }
}
//</assembly>