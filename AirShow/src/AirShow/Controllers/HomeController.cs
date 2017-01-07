using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AirShow.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using AirShow.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;
using AirShow.Models.Common;
using Microsoft.AspNetCore.Http;
using AirShow.WebSockets;

namespace AirShow.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IAppRepository _appRepository;
        private UserManager<User> _userManager;
        private GlobalWebSocketServer _gwss;

        public HomeController(IAppRepository appRepository, UserManager<User> userManager, GlobalWebSocketServer gwss)
        {
            _userManager = userManager;
            _appRepository = appRepository;
            _gwss = gwss;
        }

        public IActionResult Index()
        {
            return RedirectToAction("MyPresentations");
        }

        public async Task<IActionResult> MyActivePresentations()
        {
            var items = await _gwss.ActivePresentationsFor(_userManager.GetUserId(User));
            return View(items);
        }

        public async Task<IActionResult> MyPresentations()
        {
            var userPresentationsResult = await _appRepository.GetPresentationsForUser(_userManager.GetUserId(User), PagingOptions.FirstPageAllItems);
            var presentations = new List<MyPresentationCardModel>();
            foreach (var item in userPresentationsResult.Value)
            {
                var tagsResult = await _appRepository.GetTagsForPresentation(item);
                presentations.Add(new MyPresentationCardModel()
                {
                    Presentation = item,
                    Tags = tagsResult.Value.Select(t => t.Name).ToList()
                });
            }
            var vm = new PresentationsViewModel
            {
                Presentations = presentations
            };
            return View(vm);
        }

        public async Task<IActionResult> UploadPresentation()
        {
            var categoriesResult = await _appRepository.GetCurrentCategories();
            var vm = new UploadPresentationViewModel
            {
                ViewInput = new UploadPresentationViewModel.Input
                {
                    Categories = categoriesResult.Value
                }
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UploadPresentation(UploadPresentationViewModel vm)
        {
            Action<UploadPresentationViewModel> populateVMWithCategories = async (UploadPresentationViewModel m) =>
            {
                m.ViewInput = new UploadPresentationViewModel.Input();
                var result = await _appRepository.GetCurrentCategories();
                m.ViewInput.Categories = result.Value;
            };

            if (!ModelState.IsValid)
            {
                populateVMWithCategories(vm);
                return View(vm);
            }

            var userId = _userManager.GetUserId(User);
            var tags = vm.ViewOutput.TagsList != null ? vm.ViewOutput.TagsList.Split(new char[] { ',', ' ' }) : new string[] { };
            var tagsList = new List<string>(tags);

            var uploadModel = new UploadPresentationModel
            {
                CategoryId = vm.ViewOutput.CategoryId,
                Name = vm.ViewOutput.Name,
                Description = vm.ViewOutput.Description,
                Tags = tagsList,
                IsPublic = vm.ViewOutput.IsPublic,
                SourceStream =  vm.ViewOutput.File.OpenReadStream()
            };

            var opResult = await _appRepository.UploadPresentationForUser(userId, uploadModel);

            if (opResult.ErrorMessageIfAny != null)
            {
                populateVMWithCategories(vm);
                vm.ViewInput.ErrorMessageIfAny = opResult.ErrorMessageIfAny;
                return View(vm);
            }

            return new RedirectToActionResult("Index", "Home", new { });
        }

        public IActionResult Error()
        {
            return View();
        }


        private static OperationStatus CheckFileForMimeTypes(IFormFile fileForm)
        {
            var opResult = new OperationStatus();

            var availableContentTypes = new string[] { "application/pdf" };

            return opResult;
        }
    }
}
