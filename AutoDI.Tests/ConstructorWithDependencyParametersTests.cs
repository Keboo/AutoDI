using System;
using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.AutoMock;

namespace AutoDI.Tests
{
    [TestClass]
    public class ConstructorWithDependencyParametersTests
    {
        //TODO: These tests could bennifit from scopes

        [TestMethod]
        public void DependencyWithIntParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(42)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithIntParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithStringParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals("Test String")))).Returns(service).Verifiable();
            
            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithStringParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithNullParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0] == null))).Returns(service).Verifiable();
            
            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithNullParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithLongParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(long.MaxValue)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithLongParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithDoubleParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(double.NaN)))).Returns(service).Verifiable();
            
            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithDoubleParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithFloatParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(float.MinValue)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithFloatParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithShortParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(short.MinValue)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithShortParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithByteParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(byte.MaxValue)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithByteParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithUnsignedIntParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals((uint)int.MaxValue + 1)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithUnsignedIntParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithUnsignedLongParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals((ulong)long.MaxValue + 1)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithUnsignedLongParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithUnsignedShortParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals((ushort)short.MaxValue + 1)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithUnsignedShortParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithEnumParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(EnumParam.BeAwesome)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithEnumParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }

        [TestMethod]
        public void DependencyWithSignedByteParameter()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 1 && p[0].Equals(sbyte.MinValue)))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithSignedByteParam();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }


        [TestMethod]
        public void DependencyWithMultipleParameters()
        {
            var mocker = new AutoMocker();
            var service = mocker.Get<IService>();

            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.Is<object[]>(p => p.Length == 2 && p[0].Equals(4) && p[1].Equals("Test")))).Returns(service).Verifiable();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithTwoDependencyParams();

                Assert.AreEqual(service, sut.Service);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose();
            }
        }


    }
}