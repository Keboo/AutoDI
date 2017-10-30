using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Fody.Tests
{
    [TestClass]
    public class MatcherTests
    {
        [TestMethod]
        public void CanMatchWithSimpleWildCard()
        {
            var sut = new Matcher<string>(s => s, "Namespace.I*", "OtherNamespace.*");

            bool result = sut.TryMatch("Namespace.IService", out string replacement);
            Assert.IsTrue(result);
            Assert.AreEqual("OtherNamespace.Service", replacement);
        }

        [TestMethod]
        public void CanMatchWithRegex()
        {
            var sut = new Matcher<string>(s => s, @"regex:(.+)\.I(.+)", "regex:$1.$2");

            bool result = sut.TryMatch("Namespace.IService", out string replacement);
            Assert.IsTrue(result);
            Assert.AreEqual("Namespace.Service", replacement);
        }
    }
}