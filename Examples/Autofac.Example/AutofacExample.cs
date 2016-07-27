using AutoDI;
using ExampleClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Autofac.Example
{
    [TestClass]
    public class AutofacExample
    {
        [TestMethod]
        public void CanUseAutofac()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Service>().As<IService>();
            IContainer container = builder.Build();

            try
            {
                DependencyResolver.Set( new AutofacResolver( container ) );

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
