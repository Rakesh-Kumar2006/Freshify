using System;
using System.Linq;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    [AllowAnonymous]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // PRODUCT LIST
        [HttpGet]
        public async Task<IActionResult> Index(
            int? categoryId,
            string? search,
            int page = 1)
        {
            const int pageSize = 12;

            search = search?.Trim();

            var query = _context.Products
                .AsNoTracking()
                .AsQueryable();

            // Filter by category.
            if (categoryId.HasValue)
            {
                query = query.Where(
                    p => p.CategoryId == categoryId.Value
                );
            }

            // Search by product or category name.
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Category != null &&
                     p.Category.CategoryName.Contains(search))
                );
            }

            var totalProducts = await query.CountAsync();

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)totalProducts / pageSize
                )
            );

            page = Math.Clamp(page, 1, totalPages);

            var model = new ProductListViewModel
            {
                CategoryId = categoryId,
                Search = search,
                PageNumber = page,
                PageSize = pageSize,
                TotalProducts = totalProducts,

                Categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync(),

                Products = await query
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.ProductId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync()
            };

            return View(model);
        }

        // PRODUCT DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(
                    p => p.ProductId == id
                );

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}