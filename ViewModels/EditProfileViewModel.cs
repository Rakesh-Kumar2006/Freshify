using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Freshify.ViewModels
{
    public class EditProfileViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(250)]
        [Display(Name = "Address")]
        public string? AddressLine { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(10)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ImageFile { get; set; }

        [BindNever]
        public string? ImageUrl { get; set; }

        [BindNever]
        public DateTime CreatedAt { get; set; }
    }
}