using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.EF
{
    public class PresentationFile
    {
        [Key]
        public int Id { get; set; }

        public string FileID { get; set; }
    }
}
