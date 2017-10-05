using System;
using AutoDI;
using AutoDI.Container.Examples;
using Microsoft.AspNetCore.Mvc;

namespace Web.NetCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IQuoteService _service;

        public HomeController(IQuoteService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public IActionResult Index()
        {
            return View(_service.GetQuotes());
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}