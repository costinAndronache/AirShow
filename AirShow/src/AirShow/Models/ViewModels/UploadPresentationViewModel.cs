using AirShow.Models.EF;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class UploadPresentationViewModel
    {
        public class Input
        {
            public string ErrorMessageIfAny { get; set; }
            public List<Category> Categories { get; set; }
        }

        public class Output
        {
            [Required]
            public string Name { get; set; }
            [Required, DataType(DataType.Upload)]
            public IFormFile File { get; set; }
            [Required]
            public int CategoryId { get; set; }
            [Required, MaxLength(10000)]
            public string Description { get; set; }

            [Required]
            public bool IsPublic { get; set; }

            [Required]
            public string TagsList { get; set; }
        }

        public Input ViewInput { get; set; }
        public Output ViewOutput { get; set; }

        public string NameBeforeUpdate { get; set; }
    }
}
