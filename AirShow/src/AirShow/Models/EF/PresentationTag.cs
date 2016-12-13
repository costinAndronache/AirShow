using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.EF
{
    public class PresentationTag
    {
        public int PresentationId { get; set; }
        public Presentation Presentation { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
