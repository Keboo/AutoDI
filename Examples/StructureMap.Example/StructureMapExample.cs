using AutoDI;
using ExampleClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StructureMap.Example
{
    [TestClass]
    public class StructureMapExample
    {
        [TestMethod]
        public void CanUseStructureMap()
        {
            var container = new Container( x =>
            {
                x.For<IService>().Use<Service>();
            } );
            var service = container.GetInstance<IService>();

            try
            {
                DependencyResolver.Set( new StructureMapResolver( container ) );

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
