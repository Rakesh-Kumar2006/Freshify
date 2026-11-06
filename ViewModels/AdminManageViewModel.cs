using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Freshify.Models;
using Microsoft.AspNetCore.Http;

namespace Freshify.ViewModels
{
    // CREATE NEW ADMIN
    public class CreateAdminInputViewModel
    {
        [Required(ErrorMessage = "Enter admin full name.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter email address.")]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter password.")]
        [MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password.")]
        [Compare(nameof(Password),
            ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Profile Image")]
        public IFormFile? ImageFile { get; set; }
    }

    // ADMIN MANAGEMENT
    public class AdminManageViewModel
    {
        public List<User> Admins { get; set; } = new();

        public CreateAdminInputViewModel NewAdmin { get; set; } = new();

        public int TotalAdmins => Admins.Count;
    }
}