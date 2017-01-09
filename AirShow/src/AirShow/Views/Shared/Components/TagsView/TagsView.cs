using AirShow.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Views.Shared.Components
{
    public class TagsView: ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(TagsViewModel model)
        {
            
            return View(model);
        }
    }
}
