using System.ComponentModel.DataAnnotations;

namespace Freshify.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }

        public int UserId { get; set; }

        public int ProductId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }

        public Product? Product { get; set; }
    }
}
