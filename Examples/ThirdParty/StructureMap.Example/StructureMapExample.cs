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
            using (var container = new Container(x =>
            {
                x.For<IService>().Use<Service>();
            }))
            {
                try
                {
                    DependencyResolver.Set(new StructureMapResolver(container));

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
