﻿using System;
using AssemblyToProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
            var provider = mocker.GetMock<IServiceProvider>();
            var autoDIProvider = provider.As<IAutoDISerivceProvider>();
            autoDIProvider.Setup(x => x.GetService(typeof(IService), It.IsAny<object[]>())).Returns(service1).Verifiable();
            
            var initData = new InitData();

            try
            {
                DI.Init(typeof(IService).Assembly, builder => builder.WithProvider(provider.Object));

                var sut = new ClassWithInitData(initData);
                Assert.AreEqual(service1, sut.Service);
                Assert.AreEqual(initData, sut.Data);
                mocker.VerifyAll();
            }
            finally
            {
                DI.Dispose(typeof(IService).Assembly);
            }
        }
    }
}