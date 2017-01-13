using AirShow.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Views.Shared.Components
{
    public class PublicPresentationCard: ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(PresentationCardModel model)
        {
            return View(model);
        }
    }
}
