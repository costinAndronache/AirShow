using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AirShow.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;
using AirShow.Models.ViewModels;
using AirShow.Models.Common;
using AirShow.Utils;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    public abstract class PresentationsListController : Controller
    {
        protected IPresentationsRepository _presentationsRepository;
        protected ITagsRepository _tagsRepository;
        protected ICategoriesRepository _categoriesRepository;
        protected IUsersRepository _usersRepository;
        private IPresentationThumbnailRepository _thumbnailRepository;

        public PresentationsListController(IPresentationsRepository presentationsRepository,
                              ITagsRepository tagsRepository,
                              ICategoriesRepository categoriesRepository,
                              IUsersRepository usersRepository, 
                              IPresentationThumbnailRepository thumbnailRepository)
        {

            _thumbnailRepository = thumbnailRepository;
            _presentationsRepository = presentationsRepository;
            _tagsRepository = tagsRepository;
            _categoriesRepository = categoriesRepository;
            _usersRepository = usersRepository;
        }


        public async Task<List<PresentationCardModel>> CreateCardsModel(List<Presentation> presentationsList)
        {
            var presentations = new List<PresentationCardModel>();
            foreach (var item in presentationsList)
            {
                var tagsResult = await _tagsRepository.GetTagsForPresentation(item);
                var categoryResult = await _categoriesRepository.GetCategoryForPresentation(item);
                var usersResult = await _usersRepository.GetUsersForPresentation(item.Id, PagingOptions.CreateWithTheseOrDefaults(1, 10));
                var usersList = new List<PresentationCardModel.UserInfo>();

                if (usersResult.Value != null)
                {
                    usersList = usersResult.Value.Select(u => new PresentationCardModel.UserInfo { Name = u.Name, Href
                    = $"/{nameof(ExploreController).WithoutControllerPart()}/{nameof(ExploreController.PublicPresentationsForUser)}" + 
                    $"?userId={u.Id}&page=1&itemsPerPage=10"}).ToList();
                }

                var thumbnail = await _thumbnailRepository.GetThumbnailURLFor(item.FileID);
                presentations.Add(new PresentationCardModel()
                {
                    UserInfos = usersList,
                    Category = categoryResult.Value,
                    Presentation = item,
                    ThumbnailURL = thumbnail.Value,
                    Tags = tagsResult.Value.Select(t => t.Name).ToList()
                });
            }

            return presentations;
        }

        protected IActionResult DisplayListPage(PresentationsViewModel vm)
        {
            return View("DisplayListPage", vm);
        }

        protected IActionResult DisplayPublicListPage(PresentationsViewModel vm)
        {
            return View("DisplayPublicListPage", vm);
        }

    }
}
