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
    }
}
