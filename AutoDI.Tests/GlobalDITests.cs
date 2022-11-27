using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Tests;

[TestClass]
public class GlobalDITests
{

    [TestMethod]
    public void RegisterAddsProviders()
    {
        var provider1 = new TestProvider();
        var provider2 = new TestProvider();

        GlobalDI.Register(provider1);
        GlobalDI.Register(provider2);

        var providers = GlobalDI.Providers.ToList();
        int index1 = providers.IndexOf(provider1);
        int index2 = providers.IndexOf(provider2);
        Assert.IsTrue(index1 >= 0);
        Assert.IsTrue(index2 >= 0);
        Assert.IsTrue(index1 > index2);
    }

    [TestMethod]
    public void UnregisterRemovesProvider()
    {
        var provider = new TestProvider();
        GlobalDI.Register(provider);

        GlobalDI.Unregister(provider);

        Assert.IsFalse(GlobalDI.Providers.Contains(provider));
    }

    private class TestProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}