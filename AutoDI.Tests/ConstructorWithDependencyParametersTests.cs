using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.AutoMock;

namespace AutoDI.Tests
{
    [TestClass]
    public class ConstructorWithDependencyParametersTests
    {
        [TestMethod]
        public void WhenDependencyHasCustomAttributeWithPropertyItResolves()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var dr = mocker.GetMock<IDependencyResolver>();
            dr.Setup( x => x.Resolve<IService>( It.Is<object[]>( p => p.Length == 2 && p[0].Equals( 4 ) && p[1].Equals( "Test" ) ) ) ).Returns( service ).Verifiable();

            try
            {
                DependencyResolver.Set( dr.Object );

                var sut = new ClassWithTwoDependencyParams();

                Assert.AreEqual( service, sut.Service );
                mocker.VerifyAll();
            }
            finally
            {
                DependencyResolver.Set( (IDependencyResolver)null );
            }
        }
    }
}