using System;
using System.Collections.Generic;

namespace AutoDI
{
    public interface IContainer
    {
        event EventHandler<TypeKeyNotFoundEventArgs> TypeKeyNotFoundEvent;

        T Get<T>(IServiceProvider provider);
        object Get(Type key, IServiceProvider provider);

        IEnumerable<Map> GetMappings();

        IContainer CreatedNestedContainer();
    }
}