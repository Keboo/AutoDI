using AutoDI.Container.Examples;
using System;

namespace AutoDI.Container.ConsoleFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            var quoteBoard = new QuoteBoard();
            quoteBoard.ShowQuotes();
            Console.ReadLine();
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
                Console.WriteLine($"{quote.Text}");
                Console.WriteLine($"   -{quote.Author}");
            }
        }
    }
}
