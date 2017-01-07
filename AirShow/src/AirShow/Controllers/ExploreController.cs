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

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class ExploreController : Controller
    {
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

        public async Task<IActionResult> UserPresentationsByTag(string tag, int? page, int? itemsPerPage)
        {
            var pageIndex = page.HasValue ? page.Value : 1;
            var numOfItems = itemsPerPage.HasValue ? itemsPerPage.Value : 20;
            var id =  _userManager.GetUserId(User);
            var list = new List<MyPresentationCardModel>();
            var pagingOptions = new PagingOptions { PageIndex = pageIndex, ItemsPerPage = numOfItems };
            var presentations = await _appRepository.GetUserPresentationsFromTag(tag, id, pagingOptions);

            foreach (var p in presentations.Value)
            {
                var tagsResult = await _appRepository.GetTagsForPresentation(p);
                list.Add(new MyPresentationCardModel
                {
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

            var minPagesDisplayed = 3;
            var itemsLeftAndRight = (int)Math.Floor(minPagesDisplayed / 2.0);
            var activeItemIndex = itemsLeftAndRight; // the active item index is zero numbered

            var leftMostPage = options.PageIndex - itemsLeftAndRight;
            if (leftMostPage <= 0)
            {
                activeItemIndex = 0;
                leftMostPage = 1;
            }
            var rightMostPage = options.PageIndex + itemsLeftAndRight;

            if (rightMostPage > numOfPages)
            {
                rightMostPage = numOfPages;
                activeItemIndex = options.PageIndex - 1;
            }

            if (rightMostPage - leftMostPage + 1 < minPagesDisplayed)
            {
                if (leftMostPage == 1)
                {
                    rightMostPage += 1;
                }
                else if (rightMostPage == numOfPages)
                {
                    leftMostPage -= 1;
                }
            }

            List<string> hrefs = new List<string>();
            for(int i = leftMostPage; i <= rightMostPage; i++)
            {
                var href = "/Explore/UserPresentationsByTag?tag=" + tag + "&page=" + i + "&itemsPerPage=" + options.ItemsPerPage;
                hrefs.Add(href);
            }

            return new PaginationViewModel
            {
                Hrefs = hrefs,
                DisplayOffset = leftMostPage,
                ActiveIndex = activeItemIndex
            };
        }
    }
}
