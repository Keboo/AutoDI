using System;

namespace AutoDI
{
    public abstract class BaseResolver : IDependencyResolver
    {
        public virtual T Resolve<T>(params object[] parameters)
        {
            return (T)Resolve(typeof(T), parameters);
        }

        public abstract object Resolve(Type desiredType, params object[] parameters);
    }

    public class Derived : BaseResolver
    {
        public Derived()
        {
            //Do something
        }
        public override object Resolve(Type desiredType, params object[] parameters)
        {
            throw new NotImplementedException();
        }
    }
}