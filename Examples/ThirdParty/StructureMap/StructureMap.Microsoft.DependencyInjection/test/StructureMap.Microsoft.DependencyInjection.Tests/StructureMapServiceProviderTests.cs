using Xunit;

namespace StructureMap.Microsoft.DependencyInjection.Tests
{
    public class StructureMapServiceProviderTests
    {
        [Fact]
        public void can_start_and_tear_down_a_scope_from_the_provider()
        {
            var root = new Container(_ =>
            {
                _.For<IWidget>().Use<BlueWidget>();
            });

            var provider = new StructureMapServiceProvider(root);
            Assert.Same(provider.Container, root);
            Assert.IsType<BlueWidget>(provider.GetRequiredService(typeof(IWidget)));

            provider.StartNewScope();
            provider.Container.Configure(_ => _.For<IWidget>().Use<GreenWidget>());
            Assert.IsType<GreenWidget>(provider.GetRequiredService(typeof(IWidget)));

            provider.TeardownScope();
            Assert.IsType<BlueWidget>(provider.GetRequiredService(typeof(IWidget)));
        }

        public interface IWidget { }

        public class BlueWidget : IWidget { }
        public class GreenWidget : IWidget { }
    }
}