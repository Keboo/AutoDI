using System.Runtime.InteropServices.ComTypes;

namespace AutoDI
{
    public interface IDependencyResolver
    {
        T Resolve<T>();
    }
}