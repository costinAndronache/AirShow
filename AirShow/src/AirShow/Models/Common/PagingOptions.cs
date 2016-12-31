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

        public static PagingOptions FirstPageAllItems = new PagingOptions
        {
            PageIndex = 0,
            ItemsPerPage = int.MaxValue
        };
    }
}
