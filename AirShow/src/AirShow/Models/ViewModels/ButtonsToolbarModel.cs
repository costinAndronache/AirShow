using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class ButtonsToolbarModel
    {

        public class ButtonToolbarInfo
        {
            public string Name { get; set; }
            public string Href { get; set; }
        }


        public List<ButtonToolbarInfo> ButtonInfos { get; set; }
        public int HighlightedIndex { get; set; }



        public static ButtonsToolbarModel PublicModelWithHighlightedIndex(int index)
        {
            return new ButtonsToolbarModel
            {
                ButtonInfos = BuildPublicInfos(),
                HighlightedIndex = index
            };
        }

        public static ButtonsToolbarModel UserModelwithHighlightedIndex(int index)
        {
            return new ButtonsToolbarModel
            {
                ButtonInfos = BuildUserInfos(),
                HighlightedIndex = index
            };
        }


        public static List<ButtonToolbarInfo> BuildPublicInfos()
        {
            var list = new List<ButtonToolbarInfo>();

            list.Add(new ButtonToolbarInfo {Name = "All public", Href = "/Explore/PublicPresentations" });
            list.Add(new ButtonToolbarInfo {Name = "Software", Href = "/Explore/PublicPresentationsByCategory?categoryName=Software" });
            list.Add(new ButtonToolbarInfo { Name = "Education", Href = "/Explore/PublicPresentationsByCategory?categoryName=Education" });
            list.Add(new ButtonToolbarInfo { Name = "Sport", Href = "/Explore/PublicPresentationsByCategory?categoryName=Sport" });

            return list;
        }


        public static List<ButtonToolbarInfo> BuildUserInfos()
        {
            var list = new List<ButtonToolbarInfo>();

            list.Add(new ButtonToolbarInfo { Name = "All", Href = "/Home/MyPresentations" });
            list.Add(new ButtonToolbarInfo { Name = "Software", Href = "/Home/MyPresentationsByCategory?categoryName=Software" });
            list.Add(new ButtonToolbarInfo { Name = "Education", Href = "/Home/MyPresentationsByCategory?categoryName=Education" });
            list.Add(new ButtonToolbarInfo { Name = "Sport", Href = "/Home/MyPresentationsByCategory?categoryName=Sport" });

            return list;
        }


        public static int IndexOf(string categoryName)
        {
            int index = 0;

            switch (categoryName.ToUpper())
            {
                case "SOFTWARE":
                    index = 1;
                    break;
                case "EDUCATION":
                    index = 2;
                    break;
                case "SPORT":
                    index = 3;
                    break;
                default:
                    break;
            }

            return index;
        }

    }
}
