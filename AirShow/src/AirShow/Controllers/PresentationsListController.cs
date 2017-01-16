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

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    public abstract class PresentationsListController : Controller
    {
        protected UserManager<User> _userManager;
        protected IPresentationsRepository _presentationsRepository;
        protected ITagsRepository _tagsRepository;
        protected ICategoriesRepository _categoriesRepository;

        public PresentationsListController(IPresentationsRepository presentationsRepository,
                              ITagsRepository tagsRepository,
                              ICategoriesRepository categoriesRepository,
                              UserManager<User> userManager)
        {
            _userManager = userManager;
            _presentationsRepository = presentationsRepository;
            _tagsRepository = tagsRepository;
            _categoriesRepository = categoriesRepository;
        }


        public async Task<List<PresentationCardModel>> CreateCardsModel(List<Presentation> presentationsList)
        {
            var presentations = new List<PresentationCardModel>();
            foreach (var item in presentationsList)
            {
                var tagsResult = await _tagsRepository.GetTagsForPresentation(item);
                var categoryResult = await _categoriesRepository.GetCategoryForPresentation(item);
                presentations.Add(new PresentationCardModel()
                {
                    Category = categoryResult.Value,
                    Presentation = item,
                    Tags = tagsResult.Value.Select(t => t.Name).ToList()
                });
            }

            return presentations;
        }

        protected IActionResult DisplayListPage(PresentationsViewModel vm)
        {
            return View(vm);
        }
    }
}
