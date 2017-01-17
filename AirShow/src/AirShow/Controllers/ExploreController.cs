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

        public ExploreController(IPresentationsRepository presentationsRepository,
                              ITagsRepository tagsRepository,
                              ICategoriesRepository categoriesRepository,
                              UserManager<User> userManager): base(presentationsRepository, tagsRepository,categoriesRepository,userManager)
        {

        }
      
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> PublicPresentations(int? page, int? itemsPerPage)
        {
            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = new LeftNavbar.IndexPair
            {
                IndexWhenUserAnonymus = NavbarModel.NonAuthorizableItemsIndex.Explore,
                IndexWhenUserAuthorized = NavbarModel.AuthorizableItemsIndex.Explore
            };

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

            return base.DisplayListPage(vm);
        }


        public async Task<IActionResult> SearchPresentations(string keywords, string where, int? page, int? itemsPerPage)
        {
            var vm = new PresentationsViewModel();
            vm.NavbarIndexPair = new LeftNavbar.IndexPair
            {
                IndexWhenUserAuthorized = NavbarModel.AuthorizableItemsIndex.Explore
            };
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

            vm.Presentations = await base.CreateCardsModel(presentations.Value);
            vm.PaginationModel = await CreateSearchPaginationModel(keywordsList, searchType, presentations.TotalPages, pagingOptions);
            return base.DisplayListPage(vm);
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
            vm.NavbarIndexPair = new LeftNavbar.IndexPair
            {
                IndexWhenUserAnonymus = NavbarModel.NonAuthorizableItemsIndex.Explore,
                IndexWhenUserAuthorized = NavbarModel.AuthorizableItemsIndex.Explore
            };

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

            vm.Presentations = await base.CreateCardsModel(presentations.Value);
            vm.PaginationModel = await CreateTagPaginationModel(tag, pagingOptions, presentations.TotalPages);
            return base.DisplayListPage(vm);
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
