using System;
using System.Collections.Generic;

namespace AutoDI.Container.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new Test();
            test.RunTest();
            Console.ReadLine();
        }

        private class Test
        {
            private readonly IManager _manager;
            public Test([Dependency]IManager manager = null)
            {
                _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            }

            public void RunTest()
            {
                Console.WriteLine($"Manager = {_manager is Manager}");
                Console.WriteLine($"Service1 = {_manager.Service1 is Service1}");
                Console.WriteLine($"Service2 = {_manager.Service2 is Service2}");
            }
        }
    }
}
