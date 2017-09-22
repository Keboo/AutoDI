using System;
using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.AutoMock;

namespace AutoDI.Tests
{
    [TestClass]
    public class InheritanceTests
    {
        [TestMethod]
        public void CanFillDependenciesBeforeBaseConstructorInvocation()
        {
            var mocker = new AutoMocker();
            var service1 = mocker.Get<IService>();
            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.IsAny<object[]>())).Returns(service1).Verifiable();
            
            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithExplicitBaseDependency();
                Assert.AreEqual(service1, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose(typeof(IService).Assembly);
            }
        }
    }
}