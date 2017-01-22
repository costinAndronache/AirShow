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
using AirShow.Views.Shared.Components;
using AirShow.Utils;

namespace AirShow.Controllers
{
    [Authorize]
    public class HomeController : PresentationsListController
    {
        private UserManager<User> _userManager;

        public HomeController(IPresentationsRepository presentationsRepository,
                              ITagsRepository tagsRepository,
                              ICategoriesRepository categoriesRepository,
                              IUsersRepository usersRepository,
                              UserManager<User> userManager): base(presentationsRepository, tagsRepository,categoriesRepository, usersRepository)
        {
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (this.User.Identity != null)
            {
                return RedirectToAction(nameof(HomeController.MyPresentations));
            }
            return RedirectToAction(nameof(ExploreController.PublicPresentations), nameof(ExploreController).WithoutControllerPart(),
                new {page = 1, itemsPerPage = 5});
        }

        

        [HttpPost]
        public async Task<IActionResult> AddToMyPresentations(int presentationId)
        {
            var userId = _userManager.GetUserId(this.User);
            var opResult = await _presentationsRepository.AddPresentationToUser(presentationId, userId);

            if (opResult.ErrorMessageIfAny != null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return new JsonResult(new { errorMessage = opResult.ErrorMessageIfAny });
            }

            return new StatusCodeResult(StatusCodes.Status200OK);
        }

        public async Task<IActionResult> MyPresentations(int? page, int? itemsPerPage)
        {
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = new LeftNavbar.IndexPair
                                { IndexWhenUserAuthorized = NavbarModel.AuthorizableItemsIndex.HomeMyPresentations };
            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);

            var userPresentationsResult = await _presentationsRepository.GetPresentationsForUser(_userManager.GetUserId(User), 
                pagingOptions);

            if (userPresentationsResult.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = userPresentationsResult.ErrorMessageIfAny;
                return base.DisplayListPage(vm);
            }
            if (userPresentationsResult.Value.Count == 0)
            {
                vm.TopMessage = "You do not have any presentations. Start uploading some";
                vm.TopMessageHref = $"/{nameof(HomeController).WithoutControllerPart()}/{nameof(HomeController.UploadPresentation)}";
                return base.DisplayListPage(vm);
            }

            vm.Presentations = await base.CreateCardsModel(userPresentationsResult.Value);
            vm.PaginationModel = PaginationViewModel.BuildModelWith(userPresentationsResult.TotalPages,
                pagingOptions, index => "/Home/MyPresentations?page=" + index + "&itemsPerPage=" + pagingOptions.ItemsPerPage);

            vm.TopMessage = "My presentations";
            vm.ButtonsToolbarModel = ButtonsToolbarModel.UserModelwithHighlightedIndex(0);
            return base.DisplayListPage(vm);
        }

        public async Task<IActionResult> MyPresentationsByCategory(string categoryName, int? page, int? itemsPerPage)
        {
            var vm = new PresentationsViewModel();
            vm.Title = $"{categoryName.ToUpper()}";
            vm.ButtonsToolbarModel = ButtonsToolbarModel.UserModelwithHighlightedIndex(ButtonsToolbarModel.IndexOf(categoryName));
            vm.NavbarIndexPair = new LeftNavbar.IndexPair { IndexWhenUserAuthorized = NavbarModel.AuthorizableItemsIndex.HomeMyPresentations };


            var userId = _userManager.GetUserId(this.User);
            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var presentationsResult = await _presentationsRepository.UserPresentationsFromCategory(userId, categoryName, pagingOptions);
            if (presentationsResult.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = presentationsResult.ErrorMessageIfAny;
                return DisplayListPage(vm);
            }

            if (presentationsResult.Value.Count == 0)
            {
                vm.TopMessage = $"You do not have any presentations in the {categoryName.ToLower()} category.";
                vm.TopMessageHref = $"/{nameof(HomeController).WithoutControllerPart()}/{nameof(HomeController.UploadPresentation)}";
                return DisplayListPage(vm);
            }

            vm.TopMessage = $"Presentations in category {categoryName.ToLower()}, page {pagingOptions.PageIndex} of {presentationsResult.TotalPages}";
            vm.Presentations = await CreateCardsModel(presentationsResult.Value);
            vm.PaginationModel = PaginationViewModel.BuildModelWith(presentationsResult.TotalPages, pagingOptions, i =>
            $"/{nameof(HomeController).WithoutControllerPart()}/{nameof(HomeController.MyPresentationsByCategory)}?categpryName=" +
            $"{categoryName}&page={i}&itemsPerPage={pagingOptions.ItemsPerPage}");
            return DisplayListPage(vm);
        }

        public async Task<IActionResult> UploadPresentation()
        {
            var categoriesResult = await _categoriesRepository.GetCurrentCategories();
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
                var result = await _categoriesRepository.GetCurrentCategories();
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

            var opResult = await _presentationsRepository.UploadPresentationForUser(userId, uploadModel);
            

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
