using System.ComponentModel.DataAnnotations;

namespace Freshify.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? ProductId { get; set; }

        public int? OrderId { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        [StringLength(100)]
        public string? ReviewTitle { get; set; }

        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FeedbackType { get; set; } = "Product Review";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }

        public Product? Product { get; set; }

        public Order? Order { get; set; }
    }
}
