using AutoDI;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using ExampleClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CastleWindsor.Example
{
    [TestClass]
    public class CastleWindsorExample
    {
        [TestMethod]
        public void CanUseCastleWindsor()
        {
            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<IService>().ImplementedBy<Service>());
                try
                {
                    DependencyResolver.Set(new CastleWindsorResolver(container));

                    var @class = new Class();

                    Assert.IsTrue(@class.Service is Service);
                }
                finally
                {
                    DependencyResolver.Set((IDependencyResolver)null);
                }
            }
        }
    }
}
