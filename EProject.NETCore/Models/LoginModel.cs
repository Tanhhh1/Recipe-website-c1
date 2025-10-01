using System.ComponentModel.DataAnnotations;

namespace EProject.NETCore.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } 
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }
}
