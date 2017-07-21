using System;

namespace AutoDI
{
    public abstract class BaseResolver : IDependencyResolver
    {
        public virtual T Resolve<T>(params object[] parameters)
        {
            if (Resolve(typeof(T), parameters) is T result)
            {
                return result;
            }
            return default(T);
        }

        public abstract object Resolve(Type desiredType, params object[] parameters);
    }
}