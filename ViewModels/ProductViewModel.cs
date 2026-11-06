using System;
using System.Collections.Generic;
using Freshify.Models;

namespace Freshify.ViewModels
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public int? CategoryId { get; set; }
        public string? Search { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalProducts { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalProducts / PageSize);
    }
}