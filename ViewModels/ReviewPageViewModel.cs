using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Freshify.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Freshify.ViewModels
{
    public class ReviewPageViewModel
    {
        public int? ProductId { get; set; }

        [Display(Name = "Select Order")]
        public int? OrderId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Select a rating from 1 to 5.")]
        public int? Rating { get; set; }

        [StringLength(100)]
        [Display(Name = "Review Title")]
        public string? ReviewTitle { get; set; }

        [Required(ErrorMessage = "Please enter your feedback.")]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        [BindNever]
        public string? ProductName { get; set; }

        [BindNever]
        public List<SelectListItem> OrderOptions { get; set; } = new();

        [BindNever]
        public List<Review> Reviews { get; set; } = new();
    }
}