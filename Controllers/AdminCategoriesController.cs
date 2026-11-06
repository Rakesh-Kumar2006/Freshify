using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminCategoriesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET IMAGE FOLDER
        private string GetImageFolder()
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            return Path.Combine(
                webRoot,
                "images",
                "categories");
        }

        // VALIDATE CATEGORY IMAGE
        private static async Task<string?> ValidateImageAsync(
            IFormFile? image)
        {
            if (image == null)
                return null;

            if (image.Length == 0 ||
                image.Length > 5 * 1024 * 1024)
            {
                return "Image must be between 1 byte and 5 MB.";
            }

            string extension = Path.GetExtension(
                image.FileName).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png" &&
                extension != ".webp")
            {
                return "Only JPG, PNG and WEBP images are allowed.";
            }

            byte[] header = new byte[12];

            using var stream = image.OpenReadStream();

            int bytesRead = await stream.ReadAsync(
                header.AsMemory(0, header.Length));

            bool isJpeg = bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;

            bool isPng = bytesRead >= 8 &&
                header.Take(8).SequenceEqual(
                    new byte[]
                    {
                        137, 80, 78, 71, 13, 10, 26, 10
                    });

            bool isWebp = bytesRead >= 12 &&
                System.Text.Encoding.ASCII.GetString(
                    header, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(
                    header, 8, 4) == "WEBP";

            bool validSignature = extension switch
            {
                ".jpg" or ".jpeg" => isJpeg,
                ".png" => isPng,
                ".webp" => isWebp,
                _ => false
            };

            if (!validSignature)
                return "The selected file is not a valid image.";

            return null;
        }

        // SAVE CATEGORY IMAGE
        private async Task<string> SaveImageAsync(
            IFormFile image)
        {
            string folder = GetImageFolder();

            Directory.CreateDirectory(folder);

            string extension = Path.GetExtension(
                image.FileName).ToLowerInvariant();

            string fileName =
                Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(
                folder,
                fileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew))
            {
                await image.CopyToAsync(stream);
            }

            return "/images/categories/" + fileName;
        }

        // DELETE CATEGORY IMAGE
        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            if (!imageUrl.StartsWith(
                "/images/categories/",
                StringComparison.Ordinal))
            {
                return;
            }

            string fileName = Path.GetFileName(imageUrl);

            string filePath = Path.Combine(
                GetImageFolder(),
                fileName);

            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (IOException)
            {
                // Database operation remains successful.
            }
            catch (UnauthorizedAccessException)
            {
                // Database operation remains successful.
            }
        }

        // GET: AdminCategories/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderByDescending(c => c.CategoryId)
                .ToListAsync();

            return View(categories);
        }

        // POST: AdminCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string CategoryName,
            string? Description,
            IFormFile? ImageFile)
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                TempData["Error"] =
                    "Category name is required.";

                return RedirectToAction(nameof(Index));
            }

            CategoryName = CategoryName.Trim();

            if (CategoryName.Length > 100)
            {
                TempData["Error"] =
                    "Category name cannot exceed 100 characters.";

                return RedirectToAction(nameof(Index));
            }

            if (Description?.Length > 500)
            {
                TempData["Error"] =
                    "Description cannot exceed 500 characters.";

                return RedirectToAction(nameof(Index));
            }

            // CHECK DUPLICATE CATEGORY
            bool exists = await _context.Categories
                .AnyAsync(c => c.CategoryName == CategoryName);

            if (exists)
            {
                TempData["Error"] =
                    "This category already exists.";

                return RedirectToAction(nameof(Index));
            }

            // VALIDATE IMAGE
            string? imageError =
                await ValidateImageAsync(ImageFile);

            if (imageError != null)
            {
                TempData["Error"] = imageError;

                return RedirectToAction(nameof(Index));
            }

            string? newImage = null;

            try
            {
                // UPLOAD IMAGE IF SELECTED
                if (ImageFile != null)
                {
                    newImage = await SaveImageAsync(ImageFile);
                }

                var category = new Category
                {
                    CategoryName = CategoryName,
                    Description = Description?.Trim(),
                    ImageUrl = newImage
                };

                _context.Categories.Add(category);

                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteImage(newImage);
                throw;
            }

            TempData["Success"] =
                "Category added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: AdminCategories/Edit/1
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            var model = new CategoryFormViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };

            return View(model);
        }

        // POST: AdminCategories/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            CategoryFormViewModel model)
        {
            if (id != model.CategoryId)
                return BadRequest();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Category name is required.");
            }
            else if (model.CategoryName.Trim().Length > 100)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Category name cannot exceed 100 characters.");
            }

            // VALIDATE IMAGE
            string? imageError =
                await ValidateImageAsync(model.ImageFile);

            if (imageError != null)
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    imageError);
            }

            if (!ModelState.IsValid)
            {
                model.ImageUrl = category.ImageUrl;
                return View(model);
            }

            string categoryName = model.CategoryName.Trim();

            // CHECK DUPLICATE CATEGORY
            bool exists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryName == categoryName &&
                    c.CategoryId != id);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "This category already exists.");

                model.ImageUrl = category.ImageUrl;

                return View(model);
            }

            string? oldImage = category.ImageUrl;
            string? newImage = null;

            try
            {
                // REPLACE IMAGE IF NEW ONE IS SELECTED
                if (model.ImageFile != null)
                {
                    newImage = await SaveImageAsync(
                        model.ImageFile);

                    category.ImageUrl = newImage;
                }

                // UPDATE CATEGORY INFORMATION
                category.CategoryName = categoryName;
                category.Description = model.Description?.Trim();

                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteImage(newImage);
                throw;
            }

            // REMOVE OLD IMAGE AFTER SUCCESSFUL UPDATE
            if (newImage != null)
            {
                DeleteImage(oldImage);
            }

            TempData["Success"] =
                "Category updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: AdminCategories/Delete/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                TempData["Error"] =
                    "Category not found.";

                return RedirectToAction(nameof(Index));
            }

            // PREVENT DELETING CATEGORIES WITH PRODUCTS
            bool hasProducts = await _context.Products
                .AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
            {
                TempData["Error"] =
                    "Cannot delete this category because products are assigned to it.";

                return RedirectToAction(nameof(Index));
            }

            string? oldImage = category.ImageUrl;

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            // DELETE IMAGE AFTER CATEGORY IS REMOVED
            DeleteImage(oldImage);

            TempData["Success"] =
                "Category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}