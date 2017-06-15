using System;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
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
            var gen = new Gen();


            try
            {
                await gen.Execute(() =>
                {
                    var sut = new Sut();
                    Assert.IsTrue(sut.Service is Service);
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}

//<code_file>
namespace NS
{
    public class Foo
    {
        
    }
}

/*</code_file>*/
namespace MyTestNameSpace
{
    using System;
    using AutoDI;

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
