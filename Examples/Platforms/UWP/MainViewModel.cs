using System;
using System.Collections.ObjectModel;
using AutoDI;
using AutoDI.Container.Examples;

namespace UWP
{
    public class MainViewModel
    {
        private readonly IQuoteService _service;

        public MainViewModel([Dependency] IQuoteService service = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            LoadQuotes();
        }

        public ObservableCollection<Quote> Quotes { get; } = new ObservableCollection<Quote>();

        private void LoadQuotes()
        {
            foreach (Quote quote in _service.GetQuotes())
            {
                Quotes.Add(quote);
            }
        }
    }
}