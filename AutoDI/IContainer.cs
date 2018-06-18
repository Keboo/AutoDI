using System;
using System.Collections.Generic;

namespace AutoDI
{
    public interface IContainer : IEnumerable<Map>
    {
        event EventHandler<TypeKeyNotFoundEventArgs> TypeKeyNotFound;

        T Get<T>(IServiceProvider provider);
        object Get(Type key, IServiceProvider provider);

        IContainer CreatedNestedContainer();
    }
}