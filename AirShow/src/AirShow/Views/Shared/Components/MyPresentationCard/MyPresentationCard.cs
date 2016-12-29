using AirShow.Models.EF;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.ViewModels;

namespace AirShow.Views.Shared.Components
{
    public class MyPresentationCard: ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(MyPresentationCardModel model)
        {
            return View(model);
        }
    }
}
