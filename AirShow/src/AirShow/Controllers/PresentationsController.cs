using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AirShow.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    public class PresentationsController : Controller
    {
        private IAppRepository _appRepository;
        private UserManager<User> _userManager;

        public PresentationsController(UserManager<User> userManager, IAppRepository appRepository)
        {
            _userManager = userManager;
            _appRepository = appRepository;
        }
        // GET: /<controller>/
        [Route("/View/name")]
        public async Task<IActionResult> ViewPresentation(string name)
        {
            return View();
        }
    }
}
