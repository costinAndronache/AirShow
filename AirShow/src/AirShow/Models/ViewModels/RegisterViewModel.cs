using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(120)]
        public string Name { get; set; }

        [Required, MaxLength(80, ErrorMessage = "The email must have a maximum length of 80 characters"), DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
    }
}
