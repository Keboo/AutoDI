using System;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using AutoDI.Container.Fody;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyTestNameSpace;


namespace AutoDI.Container.Tests
{
    [TestClass]
    public class ContainerDependencies
    {
        [TestMethod]
        public async Task CanGenerateSimple()
        {
            var gen = new AssemblyGenerator.AssemblyGenerator();
            //Add AutoDI reference
            gen.AddReference(typeof(DependencyAttribute).Assembly.Location);
            gen.AddWeaver("AutoDI");
            gen.AddWeaver("AutoDI.Container");

            var testAssembly = await gen.Execute();
            testAssembly.InvokeStatic<Program>(nameof(Program.Main), new object[] {new string[0]});
            dynamic sut = testAssembly.CreateInstance<Sut>();
            Assert.IsTrue(((object)sut.Service).Is<Service>());
        }
    }
}

/*<code_file>*/
namespace MyTestNameSpace
{
    using System;
    using AutoDI;

    public class Program
    {
        public static void Main(string[] args)
        { }
    }

    public class Sut
    {
        public IService Service { get; }

        public Sut([Dependency] IService service = null)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }

    public interface IService { }

    public class Service : IService { }
}
/*</code_file>*/
