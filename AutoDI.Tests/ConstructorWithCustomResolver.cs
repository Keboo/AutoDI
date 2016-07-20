using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.AutoMock;

namespace AutoDI.Tests
{
    [TestClass]
    public class ConstructorWithCustomResolver
    {
        [TestMethod]
        public void GettingResolverPassesData()
        {
            var mocker = new AutoMocker( MockBehavior.Strict );
            var service = mocker.Get<IService>();

            var dr = mocker.GetMock<IDependencyResolver>();
            dr.Setup( x => x.Resolve<IService>() ).Returns( service ).Verifiable();

            var behavior = mocker.GetMock<IGetResolverBehavior>();
            behavior.Setup( b => b.Get( It.Is<ResolverRequest>( x => x.CallerType == typeof( ClassWithCustomResolver ) ) ) )
                .Returns( dr.Object ).Verifiable();

            try
            {
                DependencyResolver.Set( dr.Object );

                var sut = new ClassWithDependencies();

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