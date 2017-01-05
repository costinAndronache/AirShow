using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class PaginationViewModel
    {
        public List<string> Hrefs { get; set; }
        public int ActiveIndex { get; set; }
    }
}
