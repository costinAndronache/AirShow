using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class TagsViewModel
    {
        public List<string> TagsList { get; set; }

        public static TagsViewModel DummyModel = new TagsViewModel
        {
            TagsList = new List<string> {"tag1", "tag2", "tag1", "tag2", "tag1", "tag2", "tag1", "tag2", "tag1", "tag2",
            "tag1", "tag2", "tag1", "tag2", "tag1", "tag2", "tag1", "tag2", "tag1", "tag2"}
        };

    }
}
