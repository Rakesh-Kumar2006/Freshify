using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Freshify.ViewModels
{
    public class EditAdminViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Enter administrator name.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        // Existing admin profile image
        public string? ImageUrl { get; set; }

        // Upload new profile image
        [Display(Name = "Profile Image")]
        public IFormFile? ImageFile { get; set; }
    }
}