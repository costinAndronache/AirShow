using AirShow.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class PaginationViewModel
    {
        public string LeftArrowHrefIfAny { get; set; }
        public string RightArrowHrefIfAny { get; set; }
        public int DisplayOffset { get; set; }

        public List<string> Hrefs { get; set; }
        public int ActiveIndex { get; set; }


        public static PaginationViewModel BuildModelWith(int numOfPages, PagingOptions options, Func<int, string> hrefBuilderForPage)
        {
            var minPagesDisplayed = 3;
            var itemsLeftAndRight = (int)Math.Floor(minPagesDisplayed / 2.0);
            var activeItemIndex = itemsLeftAndRight; // the active item index is zero numbered

            var leftMostPage = options.PageIndex - itemsLeftAndRight;
            if (leftMostPage <= 0)
            {
                activeItemIndex = 0;
                leftMostPage = 1;
            }
            var rightMostPage = options.PageIndex + itemsLeftAndRight;

            if (rightMostPage > numOfPages)
            {
                rightMostPage = numOfPages;
                activeItemIndex = options.PageIndex - 1;
            }

            if (rightMostPage - leftMostPage + 1 < minPagesDisplayed)
            {
                if (leftMostPage == 1 && rightMostPage+1 <= numOfPages)
                {
                    rightMostPage += 1;
                }
                else if (rightMostPage == numOfPages && leftMostPage > 1)
                {
                    leftMostPage -= 1;
                }
            }

            List<string> hrefs = new List<string>();
            for (int i = leftMostPage; i <= rightMostPage; i++)
            {
                var href = hrefBuilderForPage(i);
                hrefs.Add(href);
            }

            return new PaginationViewModel
            {
                Hrefs = hrefs,
                DisplayOffset = leftMostPage,
                ActiveIndex = activeItemIndex
            };
        }
    }
    
}
