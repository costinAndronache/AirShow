using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Common
{
    public class PagingOptions
    {
        public int PageIndex { get; set; }
        public int ItemsPerPage { get; set; }

        public int ToSkip
        {
            get
            {
                return (this.PageIndex - 1) * this.ItemsPerPage;
            }
        }

        public static PagingOptions FirstPageAllItems = new PagingOptions
        {
            PageIndex = 1,
            ItemsPerPage = int.MaxValue
        };


        public static PagingOptions CreateWithTheseOrDefaults(int? page, int? itemsPerPage)
        {
            return new PagingOptions
            {
                PageIndex = page.HasValue ? (page.Value > 0 ? page.Value : 1) : 1,
                ItemsPerPage = itemsPerPage.HasValue ? (itemsPerPage.Value > 0 ? itemsPerPage.Value: 5) : 5
            };
        }
    }
}
