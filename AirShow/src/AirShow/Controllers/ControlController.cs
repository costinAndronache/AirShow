using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AirShow.WebSockets;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class ControlController : Controller
    {
        private GlobalWebSocketServer _gwss;

        public ControlController(GlobalWebSocketServer gwss)
        {
            _gwss = gwss;
        }
       
        public async Task<IActionResult> MyActivePresentations()
        {
            var items = await _gwss.ActivePresentationsFor(_userManager.GetUserId(User));
            return View(items);
        }
    }
}
