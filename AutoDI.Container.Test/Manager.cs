namespace AutoDI.Container.Test
{
    public class Manager : IManager
    {
        public IService Service1 { get; }
        public IService2 Service2 { get; }

        public Manager([Dependency]IService service1 = null, [Dependency]IService2 service2 = null)
        {
            Service1 = service1;
            Service2 = service2;
        }
    }

    public class Service1 : IService { }

    public class Service2 : IService2 { }

    public interface IManager
    {
        IService Service1 { get; }
        IService2 Service2 { get; }
    }

    public interface IService { }

    public interface IService2 { }
}