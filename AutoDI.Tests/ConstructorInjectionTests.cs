using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using System;
using System.Reflection;

namespace AutoDI.Tests
{
    [TestClass]
    public class ConstructorInjectionTests
    {
        [TestInitialize]
        public void TestSetup()
        {
            DI.Init(typeof(ClassWithDependencies).Assembly);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DI.Dispose(typeof(ClassWithDependencies).Assembly);
        }

        [TestMethod]
        public void ConstructorParametersCanBeMocked()
        {
            var mocker = new AutoMocker();
            var sut = mocker.CreateInstance<ClassWithDependencies>();

            Assert.AreEqual(mocker.Get<IService>(), sut.Service);
            Assert.AreEqual(mocker.Get<IService2>(), sut.Service2);
            //Property wont resolve because there is no resolver specified
            Assert.IsNull(sut.Service3);
        }

        [TestMethod]
        public void WhenDependencyResolverIsSpecifiedItInstance()
        {
            var mocker = new AutoMocker();
            var service1 = mocker.Get<IService>();
            var service2 = mocker.Get<IService2>();
            var service3 = mocker.Get<IService3>();
            var serviceProvider = mocker.GetMock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IService))).Returns(service1).Verifiable();
            serviceProvider.Setup(x => x.GetService(typeof(IService2))).Returns(service2).Verifiable();
            serviceProvider.Setup(x => x.GetService(typeof(IService3))).Returns(service3).Verifiable();

            DI.Dispose(typeof(ClassWithDependencies).Assembly);
            DI.Init(typeof(IService).Assembly, builder =>
            {
                builder.WithProvider(serviceProvider.Object);
            });

            var sut = new ClassWithDependencies();
            Assert.AreEqual(service1, sut.Service);
            Assert.AreEqual(service2, sut.Service2);
            Assert.AreEqual(service3, sut.Service3);
            serviceProvider.Verify();
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void WhenFirstServiceIsNotSpecifiedItThrows()
        {
            var mocker = new AutoMocker();

            new ClassWithDependencies(null, mocker.Get<IService2>());
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void WhenSecondServiceIsNotSpecifiedItThrows()
        {
            var mocker = new AutoMocker();

            new ClassWithDependencies(mocker.Get<IService>(), null);
        }
    }
}
