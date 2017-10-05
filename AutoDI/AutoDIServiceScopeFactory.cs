using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDI
{
    internal class AutoDIServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IContainer _map;

        public AutoDIServiceScopeFactory(IContainer map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public IServiceScope CreateScope()
        {
            IContainer nested = _map.CreatedNestedContainer();
            return new AutoDIServiceScope(nested);
        }

        private class AutoDIServiceScope : IServiceScope
        {
            public AutoDIServiceScope(IContainer map)
            {
                //TODO: This seems wrong....
                ServiceProvider = new AutoDIServiceProvider(map);
            }

            public void Dispose()
            {
                (ServiceProvider as IDisposable)?.Dispose();
            }

            public IServiceProvider ServiceProvider { get; }
        }
    }
}