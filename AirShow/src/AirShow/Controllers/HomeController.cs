using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AirShow.ViewComponents;
using AirShow.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace AirShow.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("MyPresentations");
        }

        public IActionResult MyActivePresentations()
        {
            return View();
        }

        public IActionResult MyPresentations()
        {
            return View();
        }

        public IActionResult UploadPresentation()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
