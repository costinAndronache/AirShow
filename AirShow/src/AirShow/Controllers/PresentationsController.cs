using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AirShow.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using AirShow.Models.ViewModels;
using AirShow.WebSockets;
using Newtonsoft.Json;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class PresentationsController : Controller
    {
        private IAppRepository _appRepository;
        private UserManager<User> _userManager;

        public PresentationsController(UserManager<User> userManager, 
                                       IAppRepository appRepository
                                       )
        {
            _userManager = userManager;
            _appRepository = appRepository;
        }
        // GET: /<controller>/
        
        public async Task<IActionResult> ViewPresentation(string name)
        {
            var activationRequest = new ActivationMessage
            {
                UserId = _userManager.GetUserId(User),
                PresentationName = name
            };

            var vm = new ViewPresentationViewModel()
            {
                PresentationURL = "/Presentations" + "/" + nameof(PresentationsController.DownloadPresentation) + "?name=" + name,
                ActivationRequestString = JsonConvert.SerializeObject(activationRequest)
            };

            return View(vm);
        }

        public async Task<IActionResult> DownloadPresentation(string name)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var userId = _userManager.GetUserId(User);
                var result = await _appRepository.DownloadPresentation(name, userId, stream);
                if (result.ErrorMessageIfAny != null)
                {
                    return NotFound();
                }

                FileContentResult fileResult = new FileContentResult(stream.ToArray(), "application/pdf");
                fileResult.FileDownloadName = name + ".pdf";
                return fileResult;
            };
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
