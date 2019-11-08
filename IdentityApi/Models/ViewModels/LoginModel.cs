using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityApi.Models.ViewModels
{
    public class LoginModel
    {
        [Required]
        [MinLength(6, ErrorMessage = "User name can not be less than 6 characters")]
        public string UserName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password can not be less than 8 characters")]
        public string Password { get; set; }
    }
}
