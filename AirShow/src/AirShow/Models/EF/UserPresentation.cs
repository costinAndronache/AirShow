using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.EF
{
    public class UserPresentation
    {
        public int PresentationId { get; set; }
        public Presentation Presentation { get; set; }


        public string UserId { get; set; }
        public User User { get; set; }
    }
}
