using AutoDI;

namespace Foo
{
    public class Class1
    {
        public Class1([Dependency] IService service = null)
        {
            
        }
    }

    public interface IService
    { }

    public class Service : IService { }
}
