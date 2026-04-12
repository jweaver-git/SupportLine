using System.ComponentModel.DataAnnotations;

namespace SupportLine.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        public string Password { get; set; }
    }
}
