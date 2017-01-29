using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Utils;

namespace AirShow.Models.ViewModels
{

    public class NavbarModel
    {

        public List<NavbarURL> URLList { get; set; }
        public int[] DisabledIndexes { get; set; }
        public int HighlightedIndex { get; set; }

        public enum AuthorizableItemsIndex
        {
            Undefined = -1,
            HomeMyPresentations = 0,
            HomeMyActivePresentations = 1,
            HomeUploadPresentation = 2,
            Explore = 3
        }

        public enum NonAuthorizableItemsIndex
        {
            Undefined = -1,
            Explore = 0,
            Login = 1,
            Register = 2
        }

        public static NavbarModel NonAuthorizableItems(int activeItemIndex = 0)
        {
            return new NavbarModel
            {
                URLList = new List<NavbarURL> { NavbarURL.ExplorePublicPresentations,
                                                NavbarURL.AccountLogin,
                                                NavbarURL.AccountRegister},
                DisabledIndexes =  new int[0],
                HighlightedIndex = activeItemIndex
            };
        }

        public static NavbarModel DefaultItems(int activeItemIndex = 0, params int[] disabledIndexes)
        {
            return new NavbarModel
            {
                URLList = new List<NavbarURL> { NavbarURL.HomeMyPresentations,
                                                NavbarURL.HomeMyActivePresentations,
                                                NavbarURL.HomeUploadPresentation,
                                                NavbarURL.ExplorePublicPresentations,
                                                NavbarURL.AccountLogout},
                HighlightedIndex = activeItemIndex,
                DisabledIndexes = disabledIndexes
            };
        }

        public class NavbarURL
        {
            public string Controller { get; set; }
            public string Action { get; set; }
            public string Name { get; set; }

            internal static NavbarURL HomeMyPresentations = new NavbarURL
            {
                Controller = nameof(Controllers.HomeController).WithoutControllerPart(),
                Action = nameof(Controllers.HomeController.MyPresentations),
                Name = "My presentations"
            };

            internal static NavbarURL HomeMyActivePresentations = new NavbarURL
            {
                Controller = nameof(Controllers.ControlController).WithoutControllerPart(),
                Action = nameof(Controllers.ControlController.MyActivePresentations),
                Name = "My active presentations"
            };

            internal static NavbarURL HomeUploadPresentation = new NavbarURL
            {
                Controller = nameof(Controllers.HomeController).WithoutControllerPart(),
                Action = nameof(Controllers.HomeController.UploadPresentation),
                Name = "Upload new presentation"
            };

            internal static NavbarURL AccountLogin = new NavbarURL
            {
                Controller = nameof(Controllers.AccountController).WithoutControllerPart(),
                Action = nameof(Controllers.AccountController.Login),
                Name = "Login"
            };

            internal static NavbarURL AccountRegister = new NavbarURL
            {
                Controller = nameof(Controllers.AccountController).WithoutControllerPart(),
                Action = nameof(Controllers.AccountController.Register),
                Name = "Register"
            };

            internal static NavbarURL AccountLogout = new NavbarURL
            {
                Controller = nameof(Controllers.AccountController).WithoutControllerPart(),
                Action = nameof(Controllers.AccountController.Logout),
                Name = "Logout"
            };

            internal static NavbarURL ExplorePublicPresentations = new NavbarURL
            {
                Controller = nameof(Controllers.ExploreController).WithoutControllerPart(),
                Action  = nameof(Controllers.ExploreController.PublicPresentations),
                Name = "Explore"
            };

        }
    }
}
