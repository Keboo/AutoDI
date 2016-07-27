using AutoDI;
using ExampleClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spring.Context.Support;
using Spring.Objects;

namespace SprintNet.Example
{
    [TestClass]
    public class SprintNetExample
    {
        [TestMethod]
        public void CanUseSprintNet()
        {
            using ( var context = new StaticApplicationContext() )
            {
                context.RegisterPrototype(typeof(IService).FullName, typeof(Service), new MutablePropertyValues());

                try
                {
                    DependencyResolver.Set( new SprintNetResolver( context ) );

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
