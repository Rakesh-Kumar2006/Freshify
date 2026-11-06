using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IWebHostEnvironment _environment;

        public AdminProfileController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            _passwordHasher = new PasswordHasher<User>();
        }

        // GET CURRENT ADMIN ID
        private int? GetCurrentAdminId()
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int id))
                return id;

            return null;
        }

        // FETCH CURRENT ADMIN FROM DATABASE
        private async Task<User?> GetCurrentAdminAsync()
        {
            int? userId = GetCurrentAdminId();

            if (!userId.HasValue)
                return null;

            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId.Value &&
                    u.Role == "Admin");
        }

        // BUILD PROFILE PAGE WITH EXISTING IMAGE
        private static AdminProfilePageViewModel BuildPage(User admin)
        {
            return new AdminProfilePageViewModel
            {
                Profile = new AdminProfileViewModel
                {
                    UserId = admin.UserId,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    Role = admin.Role,
                    ImageUrl = admin.ImageUrl
                }
            };
        }

        // GET WWWROOT PATH
        private string GetWebRootPath()
        {
            return _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
        }

        // VALIDATE PROFILE IMAGE
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
                ".jpg", ".jpeg", ".png", ".webp"
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

        // SAVE NEW PROFILE IMAGE
        private async Task<string> SaveImageAsync(IFormFile image)
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

            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew))
            {
                await image.CopyToAsync(stream);
            }

            return "/images/admins/" + fileName;
        }

        // DELETE OLD PROFILE IMAGE
        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            if (!imageUrl.StartsWith(
                "/images/admins/",
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

            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (IOException)
            {
                // Image cleanup can be retried.
            }
            catch (UnauthorizedAccessException)
            {
                // Image cleanup can be retried.
            }
        }

        // DISPLAY MY PROFILE
        // GET: /AdminProfile/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var admin = await GetCurrentAdminAsync();

            if (admin == null)
                return Forbid();

            return View(BuildPage(admin));
        }

        // UPDATE PROFILE AND IMAGE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            [Bind(Prefix = "Profile")]
            AdminProfileViewModel input)
        {
            var admin = await GetCurrentAdminAsync();

            if (admin == null)
                return Forbid();

            // Validate only profile fields
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(input.FullName))
            {
                ModelState.AddModelError(
                    "Profile.FullName",
                    "Full name is required.");
            }
            else if (input.FullName.Trim().Length > 100)
            {
                ModelState.AddModelError(
                    "Profile.FullName",
                    "Full name cannot exceed 100 characters.");
            }

            if (string.IsNullOrWhiteSpace(input.Email))
            {
                ModelState.AddModelError(
                    "Profile.Email",
                    "Email is required.");
            }
            else if (!new System.ComponentModel.DataAnnotations
                .EmailAddressAttribute()
                .IsValid(input.Email.Trim()))
            {
                ModelState.AddModelError(
                    "Profile.Email",
                    "Enter a valid email address.");
            }

            // Validate uploaded image
            string? imageError =
                await ValidateImageAsync(input.ImageFile);

            if (imageError != null)
            {
                ModelState.AddModelError(
                    "Profile.ImageFile",
                    imageError);
            }

            if (!ModelState.IsValid)
            {
                var page = BuildPage(admin);

                page.Profile.FullName = input.FullName;
                page.Profile.Email = input.Email;

                return View("Index", page);
            }

            string email =
                input.Email.Trim().ToLowerInvariant();

            // CHECK DUPLICATE EMAIL
            bool emailExists = await _context.Users
                .AnyAsync(u =>
                    u.UserId != admin.UserId &&
                    u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Profile.Email",
                    "This email is already registered.");

                var page = BuildPage(admin);

                page.Profile.FullName = input.FullName;
                page.Profile.Email = input.Email;

                return View("Index", page);
            }

            string? oldImage = admin.ImageUrl;
            string? newImage = null;

            try
            {
                // UPLOAD NEW IMAGE
                if (input.ImageFile != null)
                {
                    newImage = await SaveImageAsync(
                        input.ImageFile);

                    admin.ImageUrl = newImage;
                }

                // UPDATE NAME AND EMAIL
                admin.FullName = input.FullName.Trim();
                admin.Email = email;

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Remove newly uploaded image if saving fails
                DeleteImage(newImage);
                throw;
            }

            // DELETE PREVIOUS IMAGE AFTER SUCCESS
            if (newImage != null)
            {
                DeleteImage(oldImage);
            }

            // Refresh login only when name or email changed
            string? claimName =
                User.FindFirstValue(ClaimTypes.Name);

            string? claimEmail =
                User.FindFirstValue(ClaimTypes.Email);

            bool identityChanged =
                claimName != admin.FullName ||
                claimEmail != admin.Email;

            if (identityChanged)
            {
                await HttpContext.SignOutAsync();

                TempData["Success"] =
                    "Profile updated successfully. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] =
                "Profile updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // CHANGE PASSWORD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            [Bind(Prefix = "Password")]
            ChangeAdminPasswordViewModel input)
        {
            var admin = await GetCurrentAdminAsync();

            if (admin == null)
                return Forbid();

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(input.CurrentPassword))
            {
                ModelState.AddModelError(
                    "Password.CurrentPassword",
                    "Enter your current password.");
            }

            if (string.IsNullOrWhiteSpace(input.NewPassword) ||
                input.NewPassword.Length < 8)
            {
                ModelState.AddModelError(
                    "Password.NewPassword",
                    "New password must contain at least 8 characters.");
            }

            if (input.NewPassword != input.ConfirmPassword)
            {
                ModelState.AddModelError(
                    "Password.ConfirmPassword",
                    "Passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", BuildPage(admin));
            }

            // VERIFY CURRENT PASSWORD
            var verification =
                _passwordHasher.VerifyHashedPassword(
                    admin,
                    admin.PasswordHash,
                    input.CurrentPassword);

            if (verification == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "Password.CurrentPassword",
                    "Current password is incorrect.");

                return View("Index", BuildPage(admin));
            }

            // PREVENT REUSING CURRENT PASSWORD
            if (input.CurrentPassword == input.NewPassword)
            {
                ModelState.AddModelError(
                    "Password.NewPassword",
                    "New password must be different from the current password.");

                return View("Index", BuildPage(admin));
            }

            // SAVE NEW PASSWORD
            admin.PasswordHash =
                _passwordHasher.HashPassword(
                    admin,
                    input.NewPassword);

            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync();

            TempData["Success"] =
                "Password changed successfully. Please log in again.";

            return RedirectToAction("Login", "Account");
        }
    }
}