using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.EF
{
    public class User: IdentityUser
    {
        [Required, MaxLength(120)]
        public string Name { get; set; }

        [Required, MaxLength(120)]
        public string ActivationToken { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        public ICollection<UserPresentation> UserPresentations { get; set; }
    }
}
