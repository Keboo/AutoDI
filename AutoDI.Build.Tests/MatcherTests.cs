extern alias AutoDIBuild;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StringMatcher=AutoDIBuild::AutoDI.Build.Matcher<string>;

namespace AutoDI.Build.Tests
{
    [TestClass]
    public class MatcherTests
    {
        [TestMethod]
        public void CanMatchWithSimpleWildCard()
        {
            var sut = new StringMatcher(s => s, "Namespace.I*", "OtherNamespace.*");

            bool result = sut.TryMatch("Namespace.IService", out string replacement);
            Assert.IsTrue(result);
            Assert.AreEqual("OtherNamespace.Service", replacement);
        }

        [TestMethod]
        public void CanMatchWithRegex()
        {
            var sut = new StringMatcher(s => s, @"regex:(.+)\.I(.+)", "regex:$1.$2");

            bool result = sut.TryMatch("Namespace.IService", out string replacement);
            Assert.IsTrue(result);
            Assert.AreEqual("Namespace.Service", replacement);
        }
    }
}