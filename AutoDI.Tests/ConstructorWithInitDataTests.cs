using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;

namespace AutoDI.Tests
{
    [TestClass]
    public class ConstructorWithInitDataTests
    {
        [TestMethod]
        public void CanCreateObjectWithInitData()
        {
            var mocker = new AutoMocker();
            var service1 = mocker.Get<IService>();
            var dr = mocker.GetMock<IDependencyResolver>();
            dr.Setup( x => x.Resolve<IService>() ).Returns( service1 ).Verifiable();
            var initData = new InitData();

            try
            {
                DependencyResolver.Set( dr.Object );

                var sut = new ClassWithInitData(initData);
                Assert.AreEqual( service1, sut.Service );
                Assert.AreEqual( initData, sut.Data );
                dr.Verify();
            }
            finally
            {
                DependencyResolver.Set( null );
            }
        }
    }
}