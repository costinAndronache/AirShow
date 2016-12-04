using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{

    public class NavbarModel
    {

        public List<NavbarURL> URLList { get; set; }
        public int[] DisabledIndexes { get; set; }
        public int HighlightedIndex { get; set; }

        public enum ActiveIndex
        {
            Undefined = -1,
            HomeMyPresentations = 0,
            HomeMyActivePresentations = 1,
            HomeUploadPresentation = 2,
            AccountLogin = 0,
            AccountRegister = 1
        }

        public static NavbarModel AccountItems(int activeItemIndex = 0)
        {
            return new NavbarModel
            {
                URLList = new List<NavbarURL> { NavbarURL.AccountLogin, NavbarURL.AccountRegister},
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
                                                NavbarURL.HomeUploadPresentation},
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
                Controller = "Home",
                Action = "MyPresentations",
                Name = "My presentations"
            };

            internal static NavbarURL HomeMyActivePresentations = new NavbarURL
            {
                Controller = "Home",
                Action = "MyActivePresentations",
                Name = "My active presentation"
            };

            internal static NavbarURL HomeUploadPresentation = new NavbarURL
            {
                Controller = "Home",
                Action = "UploadPresentation",
                Name = "Upload new presentation"
            };

            internal static NavbarURL AccountLogin = new NavbarURL
            {
                Controller = "Account",
                Action = "Login",
                Name = "Login"
            };

            internal static NavbarURL AccountRegister = new NavbarURL
            {
                Controller = "Account",
                Action = "Register",
                Name = "Register"
            };
        }
    }
}
