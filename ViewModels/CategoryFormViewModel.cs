using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Freshify.ViewModels
{
    public class CategoryFormViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Display(Name = "Category Image")]
        public IFormFile? ImageFile { get; set; }
    }
}