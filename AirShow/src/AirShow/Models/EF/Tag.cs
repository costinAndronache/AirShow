using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.EF
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<PresentationTag> PresentationTags { get; set; }
    }
}
