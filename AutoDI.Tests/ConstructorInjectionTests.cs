using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.AutoMock;
using System;

namespace AutoDI.Tests
{
    [TestClass]
    public class ConstructorInjectionTests
    {
        [TestMethod]
        public void ConstructorParametersCanBeMocked()
        {
            var mocker = new AutoMocker();
            var sut = mocker.CreateInstance<ClassWithDependencies>();

            Assert.AreEqual( mocker.Get<IService>(), sut.Service );
            Assert.AreEqual( mocker.Get<IService2>(), sut.Service2 );
        }

        [TestMethod]
        public void WhenDependencyResolverIsSpecifiedItInstance()
        {
            var mocker = new AutoMocker();
            var service1 = mocker.Get<IService>();
            var service2 = mocker.Get<IService2>();
            var dr = mocker.GetMock<IDependencyResolver>();
            dr.Setup( x => x.Resolve<IService>( It.IsAny<object[]>() ) ).Returns( service1 ).Verifiable();
            dr.Setup( x => x.Resolve<IService2>( It.IsAny<object[]>() ) ).Returns( service2 ).Verifiable();

            try
            {
                DependencyResolver.Set( dr.Object );

                var sut = new ClassWithDependencies();
                Assert.AreEqual( service1, sut.Service );
                Assert.AreEqual( service2, sut.Service2 );
                dr.Verify();
            }
            finally
            {
                DependencyResolver.Set( (IDependencyResolver)null );
            }
        }

        [TestMethod, ExpectedException( typeof( ArgumentNullException ) )]
        public void WhenFirstServiceIsNotSpecifiedItThrows()
        {
            var mocker = new AutoMocker();

            new ClassWithDependencies( null, mocker.Get<IService2>() );
        }

        [TestMethod, ExpectedException( typeof( ArgumentNullException ) )]
        public void WhenSecondServiceIsNotSpecifiedItThrows()
        {
            var mocker = new AutoMocker();

            new ClassWithDependencies( mocker.Get<IService>(), null );
        }
    }
}
