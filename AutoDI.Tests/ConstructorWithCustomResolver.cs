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
        public void CanUseResolverBehaviorToCustomizeResolverInstanceReturned()
        {
            var mocker = new AutoMocker( MockBehavior.Strict );
            var service = mocker.Get<IService>();

            var dr = mocker.GetMock<IDependencyResolver>();
            dr.Setup( x => x.Resolve<IService>() ).Returns( service ).Verifiable();

            var behavior = mocker.GetMock<IGetResolverBehavior>();
            behavior.Setup( b => b.Get( It.Is<ResolverRequest>( x => x.CallerType == typeof( ClassWithSingleDependency ) && x.Dependencies.Length == 1 && x.Dependencies[0] == typeof(IService) ) ) )
                .Returns( dr.Object ).Verifiable();

            try
            {
                DependencyResolver.Set( behavior.Object );

                var sut = new ClassWithSingleDependency();

                Assert.AreEqual( service, sut.Service );
                mocker.VerifyAll();
            }
            finally
            {
                DependencyResolver.Set( (IGetResolverBehavior)null );
            }
        }
    }
}