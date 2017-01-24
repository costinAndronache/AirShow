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
using AirShow.Models.Interfaces;
using AirShow.Models.ViewModels;
using System.Net;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class ControlController : Controller
    {
        private GlobalSessionManager _gwss;
        private UserManager<User> _userManager;
        private IPresentationsRepository _presentationsRepository;
        private IPresentationThumbnailRepository _thumbnailRepository;

        public ControlController(GlobalSessionManager gwss, UserManager<User> userManager, 
                                IPresentationsRepository presentationsRepository,
                                IPresentationThumbnailRepository thumbnailRepository)
        {
            _presentationsRepository = presentationsRepository;
            _userManager = userManager;
            _gwss = gwss;
            _thumbnailRepository = thumbnailRepository;
        }
       
        public async Task<IActionResult> MyActivePresentations()
        {
            //var items = await _gwss.ActivePresentationsFor(_userManager.GetUserId(User));
            //return View(items);

            var vm = new List<ActivePresentationModel>();

            var userId = _userManager.GetUserId(this.User);
            var presentaionIds = _gwss.ActivePresentationIdsForUser(userId);
            var presentations = await _presentationsRepository.GetPresentationsWithIds(presentaionIds);

            foreach (var item in presentations.Value)
            {
                var thumbnailResult = await _thumbnailRepository.GetThumbnailURLFor(item);
                vm.Add(new ActivePresentationModel
                {
                    Presentation = item,
                    ThumbnailURL = thumbnailResult.Value
                });
            }

            return View("ActivePresentationsSessions", vm);
        }


        public async Task<IActionResult> ConnectControlForPresentation(int presentationId)
        {
            var tokenResult= _gwss.GetTokenForPresentationId(presentationId);
            return View("ConnectControlForPresentation", new ConnectControlForPresentationViewModel
            { ErrorMessage = tokenResult.ErrorMessageIfAny, SessionToken = tokenResult.Value });
        }


        [HttpDelete]
        public async Task<IActionResult> ForceStopSessionForPresentation(int presentationId)
        {
            var userId = _userManager.GetUserId(this.User);
            if (! await _presentationsRepository.UserOwnsPresentation(userId, presentationId))
            {
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            var result = _gwss.ForceStopSessionForPresentation(presentationId);
            if (result.ErrorMessageIfAny != null)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return new JsonResult(new { error = result.ErrorMessageIfAny });
            }

            return new StatusCodeResult((int)HttpStatusCode.OK);
        }

        [HttpPost]
        public async Task<IActionResult> ConnectViewForPresentation(int presentationId)
        {
            
            var userId = _userManager.GetUserId(this.User);

            if (!await _presentationsRepository.UserOwnsPresentation(userId, presentationId))
            {
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            var presentationsResult = await _presentationsRepository.GetPresentationsWithIds(new List<int> { presentationId });
            if (presentationsResult.ErrorMessageIfAny != null || presentationsResult.Value.Count == 0)
            {
                return new JsonResult(new { error = "Could not find presentation for the specified id" });
            }
            var presentation = presentationsResult.Value.First();
            var token = _gwss.ReserveNewSessionTokenFor(userId, presentation);

            return new JsonResult(new { roomToken = token });
        }


        public async Task<IActionResult> ViewPresentation(string name)
        {
            var presentationId = "";
            var userId = _userManager.GetUserId(this.User);
            var presentationResult = await _presentationsRepository.GetPresentationForUser(userId, name);
            if (presentationResult.Value != null)
            {
                presentationId = presentationResult.Value.Id + "";
            }

            var vm = new ViewPresentationViewModel()
            {
                PresentationURL = "/Presentations" + "/" + nameof(PresentationsController.DownloadPresentation) + "?name=" + name,
                ActivationRequestString = presentationId
            };

            return View(vm);
        }


        public async Task<IActionResult> SendControlMessage(string sessionToken, string message)
        {
            var userId = _userManager.GetUserId(this.User);
            var result = await _gwss.SendControlMessage(userId, sessionToken, message);
            if (result.ErrorMessageIfAny != null)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return new JsonResult(new { error = result.ErrorMessageIfAny });
            }

            return StatusCode((int)HttpStatusCode.OK);
        }

    }
}
