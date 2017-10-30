using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace StructureMap
{
    public sealed class StructureMapServiceProvider : IServiceProvider, ISupportRequiredService
    {
        private readonly Stack<IContainer> _containers = new Stack<IContainer>();

        public StructureMapServiceProvider(IContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            _containers.Push(container);
        }

        public IContainer Container => _containers.Peek();

        public object GetService(Type serviceType)
        {
            if (serviceType.IsGenericEnumerable())
            {
                // Ideally we'd like to call TryGetInstance here as well,
                // but StructureMap does't like it for some weird reason.
                return GetRequiredService(serviceType);
            }

            return Container.TryGetInstance(serviceType);
        }

        public object GetRequiredService(Type serviceType)
        {
            return Container.GetInstance(serviceType);
        }

        /// <summary>
        /// Creates a new StructureMap child container and makes that the new active container
        /// </summary>
        public void StartNewScope()
        {
            var child = Container.CreateChildContainer();
            _containers.Push(child);
        }

        /// <summary>
        /// Tears down any active child container and pops it out of the active container stack
        /// </summary>
        public void TeardownScope()
        {
            if (_containers.Count >= 2)
            {
                var child = _containers.Pop();
                child.Dispose();
            }
        }
    }
}
