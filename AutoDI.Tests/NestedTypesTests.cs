using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.AutoMock;

namespace AutoDI.Tests
{
    [TestClass]
    public class NestedTypesTests
    {
        [TestMethod]
        public void CanInjectDependenciesIntoNestedTypes()
        {
            var mocker = new AutoMocker();
            var service1 = mocker.Get<IService>();
            var dr = mocker.GetMock<IDependencyResolver>();
            dr.Setup(x => x.Resolve<IService>(It.IsAny<object[]>())).Returns(service1).Verifiable();

            try
            {
                DependencyResolver.Set(dr.Object);

                var sut = new ClassWtihNestedType.NestedType();
                Assert.AreEqual(service1, sut.Service);
                dr.Verify();
            }
            finally
            {
                DependencyResolver.Set((IDependencyResolver)null);
            }
        }
    }
}