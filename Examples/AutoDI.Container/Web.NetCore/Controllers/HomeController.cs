using System;
using AutoDI;
using AutoDI.Container.Examples;
using Microsoft.AspNetCore.Mvc;

namespace Web.NetCore.Controllers
{
    public class HomeController : Controller
    {
        private IQuoteService _service;

        public HomeController([Dependency]IQuoteService service = null)
        {
            _service = service;
            if (service == null) throw new ArgumentNullException(nameof(service));
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