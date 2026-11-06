using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IWebHostEnvironment _environment;

        public AdminManageController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            _passwordHasher = new PasswordHasher<User>();
        }

        // LOAD ADMIN ACCOUNTS
        private async Task<AdminManageViewModel> LoadAdminsAsync()
        {
            var admins = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "Admin")
                .OrderByDescending(u => u.UserId)
                .ToListAsync();

            return new AdminManageViewModel
            {
                Admins = admins
            };
        }

        // GET WWWROOT DIRECTORY
        private string GetWebRootPath()
        {
            return _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
        }

        // VALIDATE UPLOADED IMAGE
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

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                return "Only JPG, JPEG, PNG and WEBP images are allowed.";
            }

            byte[] header = new byte[12];

            using var stream = image.OpenReadStream();

            int read = await stream.ReadAsync(
                header.AsMemory(0, header.Length));

            bool isJpeg = read >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;

            bool isPng = read >= 8 &&
                header.Take(8).SequenceEqual(
                    new byte[]
                    {
                        137, 80, 78, 71, 13, 10, 26, 10
                    });

            bool isWebp = read >= 12 &&
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

            return validSignature
                ? null
                : "The selected file is not a valid image.";
        }

        // SAVE IMAGE TO WWWROOT
        private async Task<string> SaveImageAsync(
            IFormFile image)
        {
            string folder = Path.Combine(
                GetWebRootPath(),
                "images",
                "admins");

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

            return "/images/admins/" + fileName;
        }

        // DELETE PREVIOUS IMAGE
        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            const string imagePrefix = "/images/admins/";

            if (!imageUrl.StartsWith(
                imagePrefix,
                StringComparison.Ordinal))
            {
                return;
            }

            string fileName = Path.GetFileName(imageUrl);

            string filePath = Path.Combine(
                GetWebRootPath(),
                "images",
                "admins",
                fileName);

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (IOException)
                {
                    // Image cleanup can be retried later.
                }
                catch (UnauthorizedAccessException)
                {
                    // Image cleanup can be retried later.
                }
            }
        }

        // DISPLAY MANAGE ADMINS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await LoadAdminsAsync();

            return View(model);
        }

        // VIEW ADMIN DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var admin = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Admin");

            if (admin == null)
                return NotFound();

            return View(admin);
        }

        // REGISTER NEW ADMIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(Prefix = "NewAdmin")]
            CreateAdminInputViewModel input)
        {
            string? imageError =
                await ValidateImageAsync(input.ImageFile);

            if (imageError != null)
            {
                ModelState.AddModelError(
                    "NewAdmin.ImageFile",
                    imageError);
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await LoadAdminsAsync();
                invalidModel.NewAdmin = input;

                return View("Index", invalidModel);
            }

            string email =
                input.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Users
                .AnyAsync(u =>
                    u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "NewAdmin.Email",
                    "Email address is already registered.");

                var invalidModel = await LoadAdminsAsync();
                invalidModel.NewAdmin = input;

                return View("Index", invalidModel);
            }

            var admin = new User
            {
                FullName = input.FullName.Trim(),
                Email = email,
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            admin.PasswordHash =
                _passwordHasher.HashPassword(
                    admin,
                    input.Password);

            string? savedImage = null;

            try
            {
                if (input.ImageFile != null)
                {
                    savedImage = await SaveImageAsync(
                        input.ImageFile);

                    admin.ImageUrl = savedImage;
                }

                _context.Users.Add(admin);

                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteImage(savedImage);
                throw;
            }

            TempData["Success"] =
                "Administrator registered successfully.";

            return RedirectToAction(nameof(Index));
        }

        // DISPLAY EDIT ADMIN PAGE
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var admin = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Admin");

            if (admin == null)
                return NotFound();

            var model = new EditAdminViewModel
            {
                UserId = admin.UserId,
                FullName = admin.FullName,
                Email = admin.Email,
                ImageUrl = admin.ImageUrl
            };

            return View(model);
        }

        // UPDATE ADMIN INFORMATION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            EditAdminViewModel model)
        {
            if (id != model.UserId)
                return BadRequest();

            var admin = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Admin");

            if (admin == null)
                return NotFound();

            model.ImageUrl = admin.ImageUrl;

            string? imageError =
                await ValidateImageAsync(model.ImageFile);

            if (imageError != null)
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    imageError);
            }

            if (!ModelState.IsValid)
                return View(model);

            string email =
                model.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId != id &&
                    u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email address is already registered.");

                return View(model);
            }

            string? oldImage = admin.ImageUrl;
            string? newImage = null;

            try
            {
                if (model.ImageFile != null)
                {
                    newImage = await SaveImageAsync(
                        model.ImageFile);

                    admin.ImageUrl = newImage;
                }

                admin.FullName = model.FullName.Trim();
                admin.Email = email;

                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteImage(newImage);
                throw;
            }

            if (newImage != null)
            {
                DeleteImage(oldImage);
            }

            TempData["Success"] =
                "Administrator updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE ADMIN ACCOUNT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var admin = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Admin");

            if (admin == null)
            {
                TempData["Error"] =
                    "Administrator not found.";

                return RedirectToAction(nameof(Index));
            }

            // PREVENT DELETING OWN ACCOUNT
            string? loggedInUserId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                loggedInUserId,
                out int currentId))
            {
                TempData["Error"] =
                    "Cannot verify your account identity.";

                return RedirectToAction(nameof(Index));
            }

            if (currentId == id)
            {
                TempData["Error"] =
                    "You cannot delete your own account.";

                return RedirectToAction(nameof(Index));
            }

            // PREVENT DELETING LAST ADMIN
            int adminCount = await _context.Users
                .CountAsync(u => u.Role == "Admin");

            if (adminCount <= 1)
            {
                TempData["Error"] =
                    "Cannot delete the last administrator.";

                return RedirectToAction(nameof(Index));
            }

            // CHECK RELATED RECORDS
            bool hasOrders = await _context.Orders
                .AnyAsync(o => o.UserId == id);

            bool hasReviews = await _context.Reviews
                .AnyAsync(r => r.UserId == id);

            bool hasCartItems = await _context.CartItems
                .AnyAsync(c => c.UserId == id);

            if (hasOrders || hasReviews || hasCartItems)
            {
                TempData["Error"] =
                    "This administrator has related records and cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            string? imageUrl = admin.ImageUrl;

            _context.Users.Remove(admin);

            await _context.SaveChangesAsync();

            DeleteImage(imageUrl);

            TempData["Success"] =
                "Administrator deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}