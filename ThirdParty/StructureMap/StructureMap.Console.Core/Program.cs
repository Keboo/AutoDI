using System;
using AutoDI;
using ExampleClasses;
using Microsoft.Extensions.DependencyInjection;
using StructureMap.AutoDI;

namespace StructureMap.Console.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceProvider provider = DI.GetGlobalServiceProvider(typeof(Program).Assembly);
            var container = provider.GetService<Container>();
            string foo = container.WhatDoIHave();

            var service = provider.GetService<IService>();

            Program program = provider.GetService<Program>();
            program.DoStuff();
        }

        private readonly IService _service;

        public Program(IService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public void DoStuff()
        {
            
        }

        [SetupMethod]
        public static void SetupDI(IApplicationBuilder builder)
        {
            builder.UseStructureMap();
            builder.ConfigureContinaer<Registry>(registry =>
            {
                
            });
            builder.ConfigureServices(services =>
            {
                var container = new Container();
                container.Populate(services);
                //return container.GetInstance<IServiceProvider>();
            });
        }
    }
}
