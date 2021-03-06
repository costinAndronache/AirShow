﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.ViewModels;

namespace AirShow.Views.Shared.Components
{
    public class PaginationView: ViewComponent
    {
       public async Task<IViewComponentResult> InvokeAsync(PaginationViewModel model)
        {
            return View(model);
        }
    }
}
