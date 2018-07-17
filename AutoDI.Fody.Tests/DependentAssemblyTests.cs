extern alias AutoDIFody;

using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoDI.Fody.Tests
{
    using MainAssembly;
    using SharedAssembly;

    [TestClass]
    public class DependentAssemblyTests
    {
        //private static Assembly _sharedAssembly;
        private static Assembly _mainAssembly;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();
            //gen.WeaverAdded += (sender, args) =>
            //{
            //    if (args.Weaver.Name == "AutoDI")
            //    {
            //        args.Weaver.Instance.Config = XElement.Parse($@"<AutoDI {nameof(Settings.GenerateRegistrations)}=""False"" />");
            //    }
            //};

            var testAssemblies = await gen.Execute();
            
            //_sharedAssembly = testAssemblies["shared"].Assembly;
            _mainAssembly = testAssemblies["main"].Assembly;

        }

        [TestCleanup]
        public void Cleanup()
        {
            DI.Dispose(_mainAssembly);
        }

        [TestMethod]
        public void CanLoadTypesFromDependentAssemblies()
        {
            IContainer map = null;
            DI.Init(_mainAssembly, builder =>
            {
                builder.ConfigureContainer<IContainer>(container =>
                {
                    map = container;
                });
            });

            Assert.IsNotNull(map);
            Assert.IsTrue(map.IsMapped<IService, Service>(GetType()));
            Assert.IsTrue(map.IsMapped<Service, Service>(GetType()));
            Assert.IsTrue(map.IsMapped<Manager, Manager>(GetType()));
            Assert.IsTrue(map.IsMapped<Program, Program>(GetType()));
        }

        [TestMethod]
        public void DependenciesInReferencedAssembliesResolve()
        {
            _mainAssembly.InvokeEntryPoint();
            dynamic manager = _mainAssembly.GetStaticProperty<Program>(nameof(Program.Manager), GetType());
            Assert.IsNotNull(manager);
            Assert.IsNotNull(manager.Service);
        }
    }

    //<assembly:shared />
    //<weaver: AutoDI />
    //<ref: AutoDI/>
    namespace SharedAssembly
    {
        using AutoDI;

        public interface IService { }

        public class Service : IService { }

        public class Manager
        {
            public IService Service { get; }

            public Manager([Dependency] IService service = null)
            {
                Service = service;
            }
        }
    }

    //<assembly:main />
    //<type:consoleApplication />
    //<ref: shared />
    //<ref: AutoDI />
    //<weaver: AutoDI />
    namespace MainAssembly
    {
        using AutoDI;
        using SharedAssembly;

        public class Program
        {
            public static Manager Manager { get; set; }

            public static void Main(string[] args)
            {
                Manager = new Manager();
            }

            public Program([Dependency] IService service = null)
            {
                
            }
        }
    }
    //</assembly>
}



