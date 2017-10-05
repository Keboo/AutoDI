using System;
using System.Collections.Generic;

namespace AutoDI
{
    public interface IContainer
    {
        T Get<T>(IServiceProvider provider);
        object Get(Type key, IServiceProvider provider);

        IEnumerable<Map> GetMappings();

        IContainer CreatedNestedContainer();
    }
}