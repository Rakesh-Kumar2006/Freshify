using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? categoryId,
            string? search)
        {
            search = search?.Trim();

            var productsQuery = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            // Filter by the selected category.
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(
                    p => p.CategoryId == categoryId.Value
                );
            }

            // Search within the selected category, or all products.
            if (!string.IsNullOrWhiteSpace(search))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Category != null &&
                     p.Category.CategoryName.Contains(search))
                );
            }

            productsQuery = productsQuery
                .OrderByDescending(p => p.ProductId);

            // Limit only the unfiltered home page.
            if (!categoryId.HasValue &&
                string.IsNullOrWhiteSpace(search))
            {
                productsQuery = productsQuery.Take(8);
            }

            var model = new HomeViewModel
            {
                Categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync(),

                Products = await productsQuery.ToListAsync()
            };

            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Search = search;

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}