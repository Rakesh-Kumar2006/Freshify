using System.Collections.Generic;
using Freshify.Models;

namespace Freshify.ViewModels
{
    public class AdminProductsViewModel
    {
        // All products displayed in the table
        public List<Product> Products { get; set; }
            = new List<Product>();

        // Categories for Add Product dropdown
        public List<Category> Categories { get; set; }
            = new List<Category>();

        // Total products
        public int TotalProducts => Products.Count;

        // Available products
        public int AvailableProducts =>
            Products.Count(p => p.StockQuantity > 0);

        // Out of stock products
        public int OutOfStockProducts =>
            Products.Count(p => p.StockQuantity == 0);
    }
}