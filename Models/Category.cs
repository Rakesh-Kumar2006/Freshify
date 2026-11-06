using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Freshify.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Optional category image
        [StringLength(500)]
        [Display(Name = "Category Image")]
        public string? ImageUrl { get; set; }

        // Relationship with Products table
        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}