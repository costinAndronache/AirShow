﻿using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class PresentationCardModel
    {
        public class UserInfo
        {
            public string Name { get; set; }
            public string Href { get; set; }
        }

        public Presentation Presentation { get; set; } 
        public List<UserInfo> UserInfos { get; set; }
        public Category Category { get; set; }
        public List<string> Tags { get; set; }
        public string ThumbnailURL { get; set; }
    }
}
