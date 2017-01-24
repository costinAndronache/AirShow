using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AirShow.Models.Interfaces;
using AirShow.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;
using AirShow.Models.Common;
using System.Net;
using AirShow.Utils;
using AirShow.Models.ViewModels;
using AirShow.Views.Shared.Components;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class ExploreController : PresentationsListController
    {
        private static Dictionary<string, PresentationSearchType> searchTypesPerWhereValues = 
            new Dictionary<string, PresentationSearchType>()
        {
                { "name", PresentationSearchType.Name},
                { "description", PresentationSearchType.Description },
                { "tags", PresentationSearchType.Tags }
        };

        private static LeftNavbar.IndexPair defaultNavbarIndexPair = new LeftNavbar.IndexPair {IndexWhenUserAnonymus = NavbarModel.NonAuthorizableItemsIndex.Explore,
                                                                                        IndexWhenUserAuthorized = NavbarModel.AuthorizableItemsIndex.Explore};
        private UserManager<User> _userManager;

        public ExploreController(IPresentationsRepository presentationsRepository,
                              ITagsRepository tagsRepository,
                              ICategoriesRepository categoriesRepository,
                              IUsersRepository usersRepository,
                              IPresentationThumbnailRepository thumbnailRepository,
                              UserManager<User> userManager): base(presentationsRepository, tagsRepository,categoriesRepository, 
                                                                   usersRepository, thumbnailRepository)
        {
            _userManager = userManager;
        }
      
        public IActionResult Index()
        {
            return RedirectToAction(nameof(ExploreController.PublicPresentations));
        }

        [AllowAnonymous]
        public async Task<IActionResult> PublicPresentations(int? page, int? itemsPerPage)
        {
            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = defaultNavbarIndexPair;

            string excludedUserId = null;
            if (this.User != null)
            {
                excludedUserId = _userManager.GetUserId(this.User);
            }
            var presentations = await _presentationsRepository.PublicPresentations(pagingOptions, excludedUserId);
            if (presentations.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = presentations.ErrorMessageIfAny;
                return base.DisplayListPage(vm);
            }

            if (presentations.Value.Count == 0)
            {
                vm.TopMessage = "There are no other public presentations. Click here to view your own public presentations";
                vm.TopMessageHref = "/" + nameof(HomeController).WithoutControllerPart() + "/" + nameof(HomeController.MyPresentations);
                return base.DisplayListPage(vm);
            }

            vm.PaginationModel = PaginationViewModel.BuildModelWith(presentations.TotalPages, pagingOptions,
                index => "/" + nameof(ExploreController).WithoutControllerPart() + "/" + nameof(ExploreController.PublicPresentations) +  
                 "?page=" + index + "&itemsPerPage=" + pagingOptions.ItemsPerPage);

            vm.Presentations = await base.CreateCardsModel(presentations.Value);
            vm.Title = "Public Presentations";
            vm.TopMessage = $"Displaying public presentations, {(presentations.TotalPages > 0 ? presentations.TotalPages : 1)} pages in total";

            vm.ButtonsToolbarModel = ButtonsToolbarModel.PublicModelWithHighlightedIndex(0);

            return DisplayPublicListPage(vm);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PublicPresentationsByCategory(string categoryName, int? page, int? itemsPerPage)
        {
            string excludedUserId = null;
            if (this.User != null && this.User.Identity.IsAuthenticated)
            {
                excludedUserId = _userManager.GetUserId(this.User);
            }

            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = defaultNavbarIndexPair;
            vm.Title = categoryName.ToUpper();
            vm.ButtonsToolbarModel = ButtonsToolbarModel.PublicModelWithHighlightedIndex(ButtonsToolbarModel.IndexOf(categoryName));

            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var presentationsResult = await _presentationsRepository.PublicPresentationsFromCategory(categoryName, pagingOptions, 
                excludedUserId);

            if (presentationsResult.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = presentationsResult.ErrorMessageIfAny;
                return DisplayPublicListPage(vm);
            }

            if (presentationsResult.Value.Count == 0)
            {
                vm.TopMessage = $"There is no public presentation under the category {categoryName}. Be the first to upload one!";
                vm.TopMessageHref = $"/{nameof(HomeController).WithoutControllerPart()}/{nameof(HomeController.UploadPresentation)}";
                return DisplayPublicListPage(vm);
            }

            vm.PaginationModel = PaginationViewModel.BuildModelWith(presentationsResult.TotalPages, pagingOptions, i =>
            $"{nameof(ExploreController).WithoutControllerPart()}/{nameof(ExploreController.PublicPresentationsByCategory)}" + 
            $"?categoryName={categoryName}&page={i}&itemsPerPage={pagingOptions.ItemsPerPage}");

            vm.TopMessage = $"Public presentations in the {categoryName} category, page {pagingOptions.PageIndex} of {presentationsResult.TotalPages}";
            vm.Presentations = await CreateCardsModel(presentationsResult.Value);
            return DisplayPublicListPage(vm);
        }

        public async Task<IActionResult> SearchPresentations(string keywords, string where, int? page, int? itemsPerPage)
        {
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = defaultNavbarIndexPair;

            if (keywords == null || keywords.Length == 0)
            {
                vm.TopMessage = "You provided no keywords to search with";
                return base.DisplayListPage(vm);
            }

            var id = _userManager.GetUserId(User);
            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var searchType = CreateSearchType(where);
            var keywordsList = CreateKeywordsList(keywords);

            PagedOperationResult<List<Presentation>> presentations = await _presentationsRepository.
                SearchUserPresentations(keywordsList, id, pagingOptions, searchType);

            if (presentations.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = presentations.ErrorMessageIfAny;
                return base.DisplayListPage(vm);
            }

            if (presentations.Value.Count == 0)
            {
                vm.TopMessage = "The search returned no results for the keywords " + keywords;
                return base.DisplayListPage(vm);
            }


            vm.TopMessage = $"Search results for the keywords \"{keywords}\" in your presentations";
            vm.Presentations = await base.CreateCardsModel(presentations.Value);
            vm.PaginationModel = await CreateSearchPaginationModel(keywordsList, searchType, presentations.TotalPages, pagingOptions);
            return DisplayListPage(vm);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SearchPublicPresentations(string keywords, string where, int? page, int? itemsPerPage)
        {
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = defaultNavbarIndexPair;

            if (keywords == null || keywords.Length == 0)
            {
                vm.TopMessage = "You provided no keywords to search with";
                return base.DisplayListPage(vm);
            }

            string excludeUserId = null;
            if (this.User != null && this.User.Identity.IsAuthenticated)
            {
                excludeUserId = _userManager.GetUserId(this.User);
            }

            _userManager.GetUserId(User);
            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var searchType = CreateSearchType(where);
            var keywordsList = CreateKeywordsList(keywords);

            PagedOperationResult<List<Presentation>> presentations = await _presentationsRepository.
                SearchPublicPresentations(keywordsList, pagingOptions, searchType, excludeUserId);

            if (presentations.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = presentations.ErrorMessageIfAny;
                return base.DisplayListPage(vm);
            }

            if (presentations.Value.Count == 0)
            {
                vm.TopMessage = "The search returned no results for the keywords " + keywords;
                return base.DisplayListPage(vm);
            }


            vm.TopMessage = $"Search results for the keywords \"{keywords}\" in public presentations";
            vm.Presentations = await base.CreateCardsModel(presentations.Value);
            vm.PaginationModel = await CreateSearchPaginationModel(keywordsList, searchType, presentations.TotalPages, pagingOptions);
            return DisplayPublicListPage(vm);
        }

        public static PresentationSearchType CreateSearchType(string where)
        {
            if (where == null || where.Length == 0)
            {
                where = "name";
            }

            PresentationSearchType searchType = PresentationSearchType.None;
            var whereList = WebUtility.UrlDecode(where).Split(new char[] { ',', ' ' });
            foreach (var item in whereList)
            {
                if (searchTypesPerWhereValues.ContainsKey(item.ToLower()))
                {
                    searchType |= searchTypesPerWhereValues[item.ToLower()];
                }
            }

            return searchType;
        }

        public static List<string> CreateKeywordsList(string keywords)
        {
            var keywrodsList = keywords.Split(new char[] { ' ', ',' }).ToList();
            var indexOfEmpty = keywrodsList.FindIndex(item => item.Length == 0);
            if (indexOfEmpty >= 0 && indexOfEmpty < keywrodsList.Count)
            {
                keywrodsList.RemoveAt(indexOfEmpty);
            }
            return keywrodsList;
        }

        public async Task<IActionResult> UserPresentationsByTag(string tag, int? page, int? itemsPerPage)
        {
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = defaultNavbarIndexPair;

            if (tag == null || tag.Length == 0)
            {
                vm.ErrorMessage = "You have not provided any tag to search for.";
                return base.DisplayListPage(vm);
            }

            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var id =  _userManager.GetUserId(User);
            var presentations = await _presentationsRepository.GetUserPresentationsFromTag(tag, id, pagingOptions);

            if (presentations.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = presentations.ErrorMessageIfAny;
                return base.DisplayListPage(vm);
            }

            if (presentations.Value.Count == 0)
            {
                vm.TopMessage = "There are no presentations containing the tag \"" + tag + "\"";
                return base.DisplayListPage(vm);
            }

            vm.TopMessage = $"Presentations associated with \"{tag}\", page {pagingOptions.PageIndex} of {presentations.TotalPages}";
            vm.Presentations = await base.CreateCardsModel(presentations.Value);
            vm.PaginationModel = await CreateTagPaginationModel(tag, pagingOptions, presentations.TotalPages);
            return base.DisplayListPage(vm);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PublicPresentationsForUser(string userId, int? page, int? itemsPerPage)
        {
            string excludedUserId = null;
            if (this.User != null && this.User.Identity.IsAuthenticated)
            {
                excludedUserId = _userManager.GetUserId(this.User);
            }

            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = defaultNavbarIndexPair;

            

            var result = await _presentationsRepository.PublicPresentationsForUser(userId, pagingOptions, excludedUserId);
            if (result.ErrorMessageIfAny != null)
            {
                vm.ErrorMessage = result.ErrorMessageIfAny;
                return base.DisplayListPage(vm);
            }

            if (result.Value.Count == 0)
            {
                vm.TopMessage = "There are no public presentations from this user";
                return base.DisplayListPage(vm);
            }

            vm.PaginationModel = PaginationViewModel.BuildModelWith(result.TotalPages, pagingOptions, i =>
            $"{nameof(ExploreController).WithoutControllerPart()}/{nameof(ExploreController.PublicPresentationsForUser)}" + 
            $"?userId={userId}&page={i}&itemsPerPage={pagingOptions.ItemsPerPage}");

            vm.Presentations = await base.CreateCardsModel(result.Value);
            var usersResult = await _usersRepository.GetUserWithId(userId);
            if (usersResult.Value != null)
            {
                vm.TopMessage = $"Page {pagingOptions.PageIndex} of public presentations for {usersResult.Value.Name}";
            }
            return base.DisplayPublicListPage(vm);
        }

        private async Task<PaginationViewModel> CreateTagPaginationModel(string tag, PagingOptions options, int totalPages)
        {
            return PaginationViewModel.BuildModelWith(totalPages, options, index =>
            "/Explore/UserPresentationsByTag?tag=" + tag + "&page=" + index + "&itemsPerPage=" + options.ItemsPerPage);
        }


        private async Task<PaginationViewModel> CreateSearchPaginationModel(List<string> keywordsList,
                                                                            PresentationSearchType searchType,
                                                                            int numOfPages, PagingOptions currentPagingOptions)
        {
            var keywordsInOneString = "";
            keywordsList.ForEach(item => keywordsInOneString += item + ",");
            keywordsInOneString = WebUtility.UrlEncode(keywordsInOneString);

            var searchTypeString = "";
            if ((searchType & PresentationSearchType.Name) != 0)
            {
                searchTypeString += "name,"; 
            }
            if ((searchType & PresentationSearchType.Tags) != 0)
            {
                searchTypeString += "tags,";
            }
            if ((searchType & PresentationSearchType.Description) != 0)
            {
                searchTypeString += "description,";
            }
            searchTypeString = WebUtility.UrlEncode(searchTypeString);

            return PaginationViewModel.BuildModelWith(numOfPages, currentPagingOptions, index =>
            {
                return "/Explore/SearchPresentations?keywords=" + keywordsInOneString + "&where=" + searchTypeString 
                + "&page=" + index + "&itemsPerPage=" + currentPagingOptions.ItemsPerPage;
            });
        }
    }
}
