using AutoDI;
using ExampleClasses;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Unity.Example
{
    [TestClass]
    public class UnityExample
    {
        [TestMethod]
        public void CanUseUnity()
        {
            using (var container = new UnityContainer())
            {
                container.RegisterType<IService, Service>();

                try
                {
                    DependencyResolver.Set(new UnityResolver(container));

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
