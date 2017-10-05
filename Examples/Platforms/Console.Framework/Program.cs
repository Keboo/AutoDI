using System;
using AutoDI;
using AutoDI.Container.Examples;

namespace Console.Framework
{
    class Program
    {
        static void Main(string[] args)
        {
            var quoteBoard = new QuoteBoard();
            quoteBoard.ShowQuotes();
            System.Console.ReadLine();
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
