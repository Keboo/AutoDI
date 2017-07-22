using System;

namespace AutoDI
{
    public interface IDependencyResolver
    {
        T Resolve<T>(params object[] parameters);

        object Resolve(Type desiredType, params object[] parameters);
    }
}