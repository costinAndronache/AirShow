using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AirShow.WebSockets;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;
using Newtonsoft.Json;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class ControlController : Controller
    {
        private GlobalWebSocketServer _gwss;
        private UserManager<User> _userManager;

        public ControlController(GlobalWebSocketServer gwss, UserManager<User> userManager)
        {
            _userManager = userManager;
            _gwss = gwss;
        }
       
        public async Task<IActionResult> MyActivePresentations()
        {
            var items = await _gwss.ActivePresentationsFor(_userManager.GetUserId(User));
            return View(items);
        }

        public async Task<IActionResult> ControlPresentation(string name)
        {
            var vm = new ActivationMessage
            {
                PresentationName = name,
                UserId = _userManager.GetUserId(User)
            };
            return View("ControlPresentation", JsonConvert.SerializeObject(vm));
        }
    }
}
