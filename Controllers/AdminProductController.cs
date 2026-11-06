using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // DISPLAY ALL PRODUCTS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AdminProductsViewModel
            {
                Products = await _context.Products
                    .AsNoTracking()
                    .OrderByDescending(p => p.ProductId)
                    .ToListAsync(),

                Categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync()
            };

            return View(model);
        }

        // SAVE PRODUCT IMAGE
        private async Task<string> SaveImage(IFormFile image)
        {
            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            if (image.Length == 0 ||
                image.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Image size must be between 1 byte and 5 MB.");
            }

            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath, "wwwroot");

            string folder = Path.Combine(
                webRoot, "images", "products");

            Directory.CreateDirectory(folder);

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(
                filePath, FileMode.CreateNew))
            {
                await image.CopyToAsync(stream);
            }

            return "/images/products/" + fileName;
        }

        // ADD PRODUCT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string Name,
            string? Description,
            decimal Price,
            int StockQuantity,
            int CategoryId,
            string Unit,
            IFormFile? Image)
        {
            // VALIDATE PRODUCT DETAILS
            if (string.IsNullOrWhiteSpace(Name) ||
                Name.Trim().Length > 150 ||
                Price < 0 ||
                StockQuantity < 0)
            {
                TempData["Error"] =
                    "Please enter valid product details.";

                return RedirectToAction(nameof(Index));
            }

            // VALIDATE UNIT
            if (string.IsNullOrWhiteSpace(Unit) ||
                Unit.Trim().Length > 50)
            {
                TempData["Error"] =
                    "Please enter a valid product unit.";

                return RedirectToAction(nameof(Index));
            }

            // VALIDATE CATEGORY
            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == CategoryId);

            if (!categoryExists)
            {
                TempData["Error"] =
                    "Please select a valid category.";

                return RedirectToAction(nameof(Index));
            }

            // VALIDATE IMAGE
            if (Image == null)
            {
                TempData["Error"] =
                    "Please upload a product image.";

                return RedirectToAction(nameof(Index));
            }

            string imageUrl;

            try
            {
                imageUrl = await SaveImage(Image);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            // CREATE PRODUCT
            var product = new Product
            {
                Name = Name.Trim(),
                Description = Description?.Trim(),
                Price = Price,
                StockQuantity = StockQuantity,
                CategoryId = CategoryId,
                Unit = Unit.Trim(),
                ImageUrl = imageUrl
            };

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: AdminProducts/Edit/1
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(product);
        }

        // UPDATE PRODUCT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string Name,
            string? Description,
            decimal Price,
            int StockQuantity,
            int CategoryId,
            string Unit,
            IFormFile? Image)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            // VALIDATE PRODUCT DETAILS
            if (string.IsNullOrWhiteSpace(Name) ||
                Name.Trim().Length > 150 ||
                Price < 0 ||
                StockQuantity < 0)
            {
                TempData["Error"] =
                    "Please enter valid product details.";

                return RedirectToAction(nameof(Edit), new { id });
            }

            // VALIDATE UNIT
            if (string.IsNullOrWhiteSpace(Unit) ||
                Unit.Trim().Length > 50)
            {
                TempData["Error"] =
                    "Please enter a valid product unit.";

                return RedirectToAction(nameof(Edit), new { id });
            }

            // VALIDATE CATEGORY
            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == CategoryId);

            if (!categoryExists)
            {
                TempData["Error"] =
                    "Please select a valid category.";

                return RedirectToAction(nameof(Edit), new { id });
            }

            string? newImageUrl = null;

            // REPLACE IMAGE IF PROVIDED
            if (Image != null)
            {
                try
                {
                    newImageUrl = await SaveImage(Image);
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            // UPDATE PRODUCT DETAILS
            product.Name = Name.Trim();
            product.Description = Description?.Trim();
            product.Price = Price;
            product.StockQuantity = StockQuantity;
            product.CategoryId = CategoryId;
            product.Unit = Unit.Trim();

            if (newImageUrl != null)
            {
                product.ImageUrl = newImageUrl;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE PRODUCT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            // CHECK EXISTING ORDERS
            bool hasOrders = await _context.OrderItems
                .AnyAsync(i => i.ProductId == id);

            if (hasOrders)
            {
                TempData["Error"] =
                    "Cannot delete this product because it is linked to an order.";

                return RedirectToAction(nameof(Index));
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}