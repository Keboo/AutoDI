using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWithInitData
    {
        public IService Service { get; }
        public InitData Data { get; }

        public ClassWithInitData(InitData initData, [Dependency]IService service = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            Service = service;
            Data = initData;
        }
    }

    public class InitData
    { }
}