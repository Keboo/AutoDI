using AutoDI;
using ExampleClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ninject.Example
{
    [TestClass]
    public class NinjectExample
    {
        [TestMethod]
        public void CanUseNinject()
        {
            using ( IKernel kernel = new StandardKernel() )
            {
                kernel.Bind<IService>().To<Service>();

                try
                {
                    DependencyResolver.Set( new NinjectResolver( kernel ) );

                    var @class = new Class();

                    Assert.IsTrue( @class.Service is Service );
                }
                finally
                {
                    DependencyResolver.Set( (IDependencyResolver)null );
                }
            }
        }
    }
}
