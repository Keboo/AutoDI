using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using System;
using Moq;

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

        [TestMethod]
        public void WhenDependencyResolveBehaviorIsSpecifiedItUsesIt()
        {
            var mocker = new AutoMocker();
            var service1 = mocker.Get<IService>();
            var service2 = mocker.Get<IService2>();
            var dr = mocker.GetMock<IDependencyResolver>();
            dr.Setup( x => x.Resolve<IService>( It.IsAny<object[]>() ) ).Returns( service1 ).Verifiable();
            dr.Setup( x => x.Resolve<IService2>( It.IsAny<object[]>() ) ).Returns( service2 ).Verifiable();
            var behavior = mocker.GetMock<IGetResolverBehavior>();
            behavior.Setup( x => x.Get( It.IsAny<ResolverRequest>() ) ).Returns( dr.Object );

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
                DependencyResolver.Set( (IGetResolverBehavior)null );
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

        private void Test()
        {
            var resolver = new Resolver();
            
            resolver.Resolve<IService>(0, "Kevin");
            resolver.Resolve<IService2>();
        }


        private class Resolver : IDependencyResolver
        {
            public T Resolve<T>(params object[] parameters)
            {
                return default(T);
            }
        }
    }
}
