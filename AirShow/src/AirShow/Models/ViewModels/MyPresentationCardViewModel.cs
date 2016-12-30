using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class MyPresentationCardModel
    {
        public Presentation Presentation { get; set; }
        public List<string> Tags { get; set; }
        public string ThumbnailURL { get; set; }
    }
}
