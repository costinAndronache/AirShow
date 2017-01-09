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

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class ExploreController : Controller
    {
        private static Dictionary<string, PresentationSearchType> searchTypesPerWhereValues = 
            new Dictionary<string, PresentationSearchType>()
        {
                { "name", PresentationSearchType.Name},
                { "description", PresentationSearchType.Description },
                { "tags", PresentationSearchType.Tags }
        };
        private IAppRepository _appRepository;
        private UserManager<User> _userManager;

       public ExploreController(IAppRepository appRepository, UserManager<User> userManager)
        {
            _userManager = userManager;
            _appRepository = appRepository;
        }

        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> SearchPresentations(string keywords, string where, int? page, int? itemsPerPage)
        {
            if (keywords == null || keywords.Length == 0)
            {
                return View(new PresentationsViewModel
                {
                    ErrorMessage = "Please provide at least a keyword in your search criteria"
                });
            }

            if (where == null || where.Length == 0)
            {
                where = "name";
            }

            var pageIndex = page.HasValue ? page.Value : 1;
            var numOfItems = itemsPerPage.HasValue ? itemsPerPage.Value : 1;
            var id = _userManager.GetUserId(User);
            var list = new List<MyPresentationCardModel>();
            var pagingOptions = new PagingOptions { PageIndex = pageIndex, ItemsPerPage = numOfItems };
            var keywrodsList = keywords.Split(new char[] {' ', ','}).ToList();
            var indexOfEmpty = keywrodsList.FindIndex(item => item.Length == 0);
            if (indexOfEmpty >= 0 && indexOfEmpty < keywrodsList.Count)
            {
                keywrodsList.RemoveAt(indexOfEmpty);
            }

            PresentationSearchType searchType = PresentationSearchType.None;
            var whereList = WebUtility.UrlDecode(where).Split(new char[] {',', ' '});
            foreach (var item in whereList)
            {
                if (searchTypesPerWhereValues.ContainsKey(item.ToLower()))
                {
                    searchType |= searchTypesPerWhereValues[item.ToLower()];
                }
            }

            PagedOperationResult<List<Presentation>> presentations = await _appRepository.SearchUserPresentations(keywrodsList,
                id, pagingOptions, searchType);

            var vmList = new List<MyPresentationCardModel>();
            foreach (var item in presentations.Value)
            {
                var tagsResult = await _appRepository.GetTagsForPresentation(item);
                var categoryResult = await _appRepository.GetCategoryForPresentation(item);
                vmList.Add(new MyPresentationCardModel { Category = categoryResult.Value,
                    Presentation = item, Tags = tagsResult.Value.Select(t => t.Name).ToList()});
            }

            return View(new PresentationsViewModel
            {
                Presentations = vmList,
                PaginationModel = await CreateSearchPaginationModel(keywrodsList, searchType, presentations.TotalPages, pagingOptions)
            });
        }

        public async Task<IActionResult> UserPresentationsByTag(string tag, int? page, int? itemsPerPage)
        {
            if (tag == null || tag.Length == 0)
            {
                return View(new PresentationsViewModel
                {
                    ErrorMessage = "Please provide a tag for your search"
                });
            }

            var pageIndex = page.HasValue ? page.Value : 1;
            var numOfItems = itemsPerPage.HasValue ? itemsPerPage.Value : 20;
            var id =  _userManager.GetUserId(User);
            var list = new List<MyPresentationCardModel>();
            var pagingOptions = new PagingOptions { PageIndex = pageIndex, ItemsPerPage = numOfItems };
            var presentations = await _appRepository.GetUserPresentationsFromTag(tag, id, pagingOptions);

            foreach (var p in presentations.Value)
            {
                var tagsResult = await _appRepository.GetTagsForPresentation(p);
                var categoryResult = await _appRepository.GetCategoryForPresentation(p);
                list.Add(new MyPresentationCardModel
                {
                    Category = categoryResult.Value,
                    Presentation = p,
                    Tags = tagsResult.Value.Select(t => t.Name).ToList()
                });
            }

            return View(new PresentationsViewModel {
                Presentations = list,
                PaginationModel = await CreateTagPaginationModel(tag, pagingOptions)
            });
        }


        private async Task<PaginationViewModel> CreateTagPaginationModel(string tag, PagingOptions options)
        {
            var userId = _userManager.GetUserId(User);
            var numOfItems = await _appRepository.GetNumberOfUserPresentationsWithTag(tag, userId);
            var numOfPages = numOfItems.Value / options.ItemsPerPage;

            return PaginationViewModel.BuildModelWith(numOfPages, options, index =>
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
