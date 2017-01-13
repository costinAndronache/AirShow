using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.ViewModels;

namespace AirShow.Views.Shared.Components
{
    public class LeftNavbar: ViewComponent
    {
        public class IndexPair
        {
            public NavbarModel.AuthorizableItemsIndex IndexWhenUserAuthorized { get; set; }
            public NavbarModel.NonAuthorizableItemsIndex IndexWhenUserAnonymus { get; set; }
        }

        public async Task<IViewComponentResult> InvokeAsync(IndexPair indexPair)
        {
            NavbarModel model = null;

            if (User.Identity.IsAuthenticated)
            {
                model = NavbarModel.DefaultItems((int)indexPair.IndexWhenUserAuthorized);
            }
            else
            {
                model = NavbarModel.NonAuthorizableItems((int)indexPair.IndexWhenUserAnonymus);
            }

            //debug
            //model = NavbarModel.DefaultItems((int)activeIndex);
            //
            return View(model);
        }
    }
}
