using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class MultipleAnnotatedConstructorTest
    {
        [TestMethod]
        public async Task MultipleAnnotatedConstructorsResultsInCompileError()
        {
            var gen = new Generator();

            try
            {
                await gen.Execute();
            }
            catch (WeaverErrorException e) when (e.Errors?.Any(x => x.Contains("More then one constructor on 'MultipleDiAnnotatedManager' annotated with DiConstructorAttribute")) == true)
            {
                return;
            }

            Assert.Fail("Expected compile error");
        }
    }

    //<assembly>
    //<ref: AutoDI />
    //<weaver: AutoDI.Build.ProcessAssemblyTask />
    namespace MultipleAnnotatedConstructorTestsNamespace
    {
        using AutoDI;

        public interface IService1
        { }

        public class MultipleDiAnnotatedManager
        {
            public IService1 Service1 { get; }


            [DiConstructor]
            public MultipleDiAnnotatedManager()
            {
            }

            [DiConstructor]
            public MultipleDiAnnotatedManager(IService1 service1)
            {
                Service1 = service1;
            }
        }
    }
    //</assembly>
}