using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.ViewModels;

namespace AirShow.Views.Shared.Components
{
    public class ModalMessageView: ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(ModalMessageViewModel model)
        {
            return View(model);
        }
    }
}
