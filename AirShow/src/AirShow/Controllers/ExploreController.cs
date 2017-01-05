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

            var presentations = await _appRepository.GetUserPresentationsFromTag(tag, id, 
                new Models.Common.PagingOptions { PageIndex = pageIndex, ItemsPerPage = numOfItems });

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
                Presentations = list
            });
        }

    }
}
