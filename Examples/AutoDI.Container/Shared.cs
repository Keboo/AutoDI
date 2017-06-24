using System.Collections.Generic;

namespace AutoDI.Container.Examples
{
    public interface IQuoteService
    {
        IEnumerable<Quote> GetQuotes();
    }

    public class QuoteService : IQuoteService
    {
        public IEnumerable<Quote> GetQuotes()
        {
            yield return new Quote("Author", "Some text");
        }
    }

    public class Quote
    {
        public Quote(string author, string text)
        {
            Author = author;
            Text = text;
        }

        public string Author { get; }

        public string Text { get; }
    }
}