using System;
using AutoDI;
using AutoDI.Container.Examples;
using Microsoft.Extensions.DependencyInjection;

namespace Console.NetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var quoteBoard = new QuoteBoard();
            quoteBoard.ShowQuotes();
            System.Console.ReadLine();
        }

        private static IServiceProvider _globalServiceProvider;

        public static void Init(Action<IApplicationBuilder> configure)
        {
            if (_globalServiceProvider != null)
                throw new AlreadyInitializedException();
            IApplicationBuilder applicationBuilder = new ApplicationBuilder();
            applicationBuilder.ConfigureServices(Gen_Configured);
            if (configure != null)
                configure(applicationBuilder);
            _globalServiceProvider = applicationBuilder.Build();
        }

        private static PrincessBrideQuoteService PrincessBrideQuoteService_generated_0(IServiceProvider serviceProvider)
        {
            return new PrincessBrideQuoteService();
        }

        private static void Gen_Configured(IServiceCollection collection)
        {
            collection.AddAutoDIService<PrincessBrideQuoteService>(PrincessBrideQuoteService_generated_0, new[]
            {
                typeof (IQuoteService),
                typeof (PrincessBrideQuoteService)
            }, Lifetime.LazySingleton);
        }
    }

    public class QuoteBoard
    {
        private readonly IQuoteService _service;

        public QuoteBoard([Dependency]IQuoteService service = null)
        {
            _service = service;
            if (service == null) throw new ArgumentNullException(nameof(service));
        }

        public void ShowQuotes()
        {
            foreach (Quote quote in _service.GetQuotes())
            {
                System.Console.WriteLine($"{quote.Text}");
                System.Console.WriteLine($"   -{quote.Author}");
                System.Console.WriteLine();
            }
        }
    }
}