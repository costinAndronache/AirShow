using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.EF
{
    public class Presentation
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Name { get; set; }

        [Required, MaxLength(10000)]
        public string Description { get; set; }

        [Required]
        public bool IsPublic { get; set; }

        [Required, MaxLength(255)]
        public string FileId { get; set; }
        
        public DateTime UploadedDate { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<PresentationTag> PresentationTags { get; set; }
        public ICollection<UserPresentation> UserPresentations { get; set; }
    }
}
