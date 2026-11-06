using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Freshify.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required, StringLength(30)]
        public string Unit { get; set; } = "Piece";

        public string ImageUrl { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();
    }
}
