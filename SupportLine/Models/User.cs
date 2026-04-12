using System.ComponentModel.DataAnnotations;

namespace SupportLine.Models
{
    public class User
    {
        public int UserID { get; set; }
        
        // The [Required] tags ensure that the fields are not left empty by the user
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(100)]
        public string Name { get; set; }

        // The [EmailAddress] tag ensures that the email is in a valid format
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please create a password.")]
        public string Password { get; set; }

        public string Role { get; set; }
    }
}
