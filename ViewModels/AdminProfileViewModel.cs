using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Freshify.ViewModels
{
    public class AdminProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        public string Email { get; set; } = "";

        public string Role { get; set; } = "Admin";

        // Existing image from database
        public string? ImageUrl { get; set; }

        // New uploaded image
        [Display(Name = "Profile Image")]
        public IFormFile? ImageFile { get; set; }
    }

    public class ChangeAdminPasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }

    public class AdminProfilePageViewModel
    {
        public AdminProfileViewModel Profile { get; set; } = new();

        public ChangeAdminPasswordViewModel Password { get; set; } = new();
    }
}