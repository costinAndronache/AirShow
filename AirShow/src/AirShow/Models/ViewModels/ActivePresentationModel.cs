using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class ActivePresentationModel
    {
        public Presentation Presentation { get; set; }
        public string ThumbnailURL { get; set; }
    }
}
