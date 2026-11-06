using System.Collections.Generic;
using System.Linq;

namespace Freshify.ViewModels
{
    public class CartViewModel
    {
        public List<CartLineViewModel> Items { get; set; } = new();

        public decimal Total =>
            Items.Sum(item => item.Subtotal);
    }

    public class CartLineViewModel
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string Unit { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public int StockQuantity { get; set; }

        public decimal Subtotal => Price * Quantity;
    }
}