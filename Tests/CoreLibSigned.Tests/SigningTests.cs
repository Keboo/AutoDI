using AssemblyToProcess;
using Castle.DynamicProxy.Generators.Emitters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreLibSigned.Tests
{
    [TestClass]
    public class SigningTests
    {
        [TestMethod]
        public void AssemblyIsSigned()
        {
            Assert.IsTrue(typeof(DISetup).Assembly.IsAssemblySigned());
        }
    }
}