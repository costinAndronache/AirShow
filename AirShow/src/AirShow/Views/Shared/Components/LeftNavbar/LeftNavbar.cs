using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.ViewModels;

namespace AirShow.ViewComponents.LeftNavbarViewComponent
{
    public class LeftNavbar: ViewComponent
    {

        public async Task<IViewComponentResult> InvokeAsync(NavbarModel.ActiveIndex activeIndex = NavbarModel.ActiveIndex.Undefined)
        {
            NavbarModel model = null;

            if (User.Identity.IsAuthenticated)
            {
                model = NavbarModel.DefaultItems((int)activeIndex);
            }
            else
            {
                model = NavbarModel.AccountItems((int)activeIndex);
            }

            //debug
            //model = NavbarModel.DefaultItems((int)activeIndex);
            //
            return View(model);
        }
    }
}
