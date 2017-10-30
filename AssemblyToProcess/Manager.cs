using AutoDI;

namespace AssemblyToProcess
{
    public class Manager : IManager
    {
        public IService Service1 { get; }
        public IService2 Service2 { get; }

        [DiConstructor]
        public Manager([Dependency]IService service1 = null, [Dependency]IService2 service2 = null)
        {
            Service1 = service1;
            Service2 = service2;
        }
    }

    public interface IManager { }
}