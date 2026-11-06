using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    public class CustomerAccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly PasswordHasher<User> _hasher = new();

        public CustomerAccountController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // LOGIN PAGE
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // PROCESS LOGIN
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string? email,
            string? password)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            email = email?.Trim();
            ViewBag.Email = email;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Enter your email and password.";
                return View();
            }

            var customer = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.Role == "Customer");

            if (customer == null ||
                string.IsNullOrWhiteSpace(customer.PasswordHash))
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            var result = _hasher.VerifyHashedPassword(
                customer,
                customer.PasswordHash,
                password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (result ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                customer.PasswordHash =
                    _hasher.HashPassword(customer, password);

                await _context.SaveChangesAsync();
            }

            await SignInCustomer(
                customer,
                new AuthenticationProperties
                {
                    IsPersistent = false
                }
            );

            return RedirectToAction("Index", "Home");
        }

        // PROFILE PAGE
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MyProfile()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!User.IsInRole("Customer"))
            {
                return StatusCode(403);
            }

            var customerId = GetCustomerId();

            if (!customerId.HasValue)
            {
                return await ClearInvalidLogin();
            }

            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == customerId.Value &&
                    u.Role == "Customer");

            if (customer == null)
            {
                return await ClearInvalidLogin();
            }

            var model = new EditProfileViewModel
            {
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                AddressLine = customer.AddressLine,
                City = customer.City,
                State = customer.State,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                ImageUrl = customer.ImageUrl,
                CreatedAt = customer.CreatedAt
            };

            return View(model);
        }

        // SAVE PROFILE CHANGES
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(
            EditProfileViewModel model)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!User.IsInRole("Customer"))
            {
                return StatusCode(403);
            }

            var customerId = GetCustomerId();

            if (!customerId.HasValue)
            {
                return await ClearInvalidLogin();
            }

            // Always load the customer from the signed-in user's ID.
            var customer = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == customerId.Value &&
                    u.Role == "Customer");

            if (customer == null)
            {
                return await ClearInvalidLogin();
            }

            // Restore display-only values from the database.
            model.ImageUrl = customer.ImageUrl;
            model.CreatedAt = customer.CreatedAt;

            model.FullName =
                model.FullName?.Trim() ?? string.Empty;

            model.Email =
                model.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError(
                    nameof(model.FullName),
                    "Full name is required."
                );
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists = await _context.Users
                    .AnyAsync(u =>
                        u.UserId != customer.UserId &&
                        u.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "This email is already registered."
                    );
                }
            }

            var image = await ValidatePhoto(
                model.ImageFile,
                nameof(model.ImageFile)
            );

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? savedFilePath = null;

            try
            {
                if (image.Bytes != null)
                {
                    var savedPhoto = await SavePhoto(
                        image.Bytes,
                        image.Extension!
                    );

                    savedFilePath = savedPhoto.FilePath;
                    customer.ImageUrl = savedPhoto.ImageUrl;
                }

                // Update only editable profile fields.
                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Phone =
                    model.Phone?.Trim() ?? string.Empty;

                customer.AddressLine = model.AddressLine?.Trim();
                customer.City = model.City?.Trim();
                customer.State = model.State?.Trim();
                customer.PostalCode = model.PostalCode?.Trim();
                customer.Country = model.Country?.Trim();

                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteUploadedPhoto(savedFilePath);
                throw;
            }

            // Refresh the authentication cookie with the new name/email.
            var authentication = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await SignInCustomer(
                customer,
                authentication.Properties ??
                new AuthenticationProperties
                {
                    IsPersistent = false
                }
            );

            TempData["Success"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(MyProfile));
        }

        // REGISTRATION PAGE
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new CustomerRegisterViewModel());
        }

        // PROCESS REGISTRATION
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            CustomerRegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            model.FullName =
                model.FullName?.Trim() ?? string.Empty;

            model.Email =
                model.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError(
                    nameof(model.FullName),
                    "Full name is required."
                );
            }

            var image = await ValidatePhoto(
                model.ImageFile,
                nameof(model.ImageFile)
            );

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered."
                );

                return View(model);
            }

            var customer = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone?.Trim() ?? string.Empty,
                AddressLine = model.AddressLine?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                PostalCode = model.PostalCode?.Trim(),
                Country = model.Country?.Trim(),

                // Role is assigned only on the server.
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };

            customer.PasswordHash = _hasher.HashPassword(
                customer,
                model.Password
            );

            string? savedFilePath = null;

            try
            {
                if (image.Bytes != null)
                {
                    var savedPhoto = await SavePhoto(
                        image.Bytes,
                        image.Extension!
                    );

                    savedFilePath = savedPhoto.FilePath;
                    customer.ImageUrl = savedPhoto.ImageUrl;
                }

                _context.Users.Add(customer);
                await _context.SaveChangesAsync();
            }
            catch
            {
                DeleteUploadedPhoto(savedFilePath);
                throw;
            }

            TempData["Success"] =
                "Registration successful! Please log in.";

            return RedirectToAction(nameof(Login));
        }

        // CUSTOMER LOGOUT
        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Index", "Home");
        }

        // CURRENT CUSTOMER ID
        private int? GetCustomerId()
        {
            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier
            )?.Value;

            return int.TryParse(claim, out var id) && id > 0
                ? id
                : null;
        }

        // CLEAR AN INVALID LOGIN
        private async Task<IActionResult> ClearInvalidLogin()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(nameof(Login));
        }

        // CREATE OR REFRESH CUSTOMER LOGIN COOKIE
        private async Task SignInCustomer(
            User customer,
            AuthenticationProperties properties)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    customer.UserId.ToString()
                ),
                new Claim(ClaimTypes.Name, customer.FullName),
                new Claim(ClaimTypes.Email, customer.Email),
                new Claim(ClaimTypes.Role, "Customer")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties
            );
        }

        // VALIDATE OPTIONAL PROFILE PHOTO
        private async Task<(byte[]? Bytes, string? Extension)>
            ValidatePhoto(IFormFile? file, string fieldName)
        {
            if (file == null)
            {
                return (null, null);
            }

            const long maximumSize = 5 * 1024 * 1024;

            if (file.Length <= 0 || file.Length > maximumSize)
            {
                ModelState.AddModelError(
                    fieldName,
                    "Select an image between 1 byte and 5 MB."
                );

                return (null, null);
            }

            var extension = Path.GetExtension(
                file.FileName
            ).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                ModelState.AddModelError(
                    fieldName,
                    "Only JPG, JPEG, and PNG images are allowed."
                );

                return (null, null);
            }

            using var memory = new MemoryStream();
            await file.CopyToAsync(memory);

            var bytes = memory.ToArray();

            var pngSignature = new byte[]
            {
                137, 80, 78, 71, 13, 10, 26, 10
            };

            var isPng = bytes.Length >= 8 &&
                bytes.Take(8).SequenceEqual(pngSignature);

            var isJpeg = bytes.Length >= 3 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xD8 &&
                bytes[2] == 0xFF;

            var matchesType =
                (extension == ".png" && isPng) ||
                ((extension == ".jpg" || extension == ".jpeg")
                    && isJpeg);

            if (!matchesType)
            {
                ModelState.AddModelError(
                    fieldName,
                    "The file must be a JPG or PNG image."
                );

                return (null, null);
            }

            return (bytes, isPng ? ".png" : ".jpg");
        }

        // SAVE PHOTO USING A SERVER-GENERATED FILE NAME
        private async Task<(string FilePath, string ImageUrl)>
            SavePhoto(byte[] bytes, string extension)
        {
            var webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"
                );

            var uploadFolder = Path.Combine(
                webRoot,
                "images",
                "users"
            );

            Directory.CreateDirectory(uploadFolder);

            var fileName =
                Guid.NewGuid().ToString("N") + extension;

            var filePath = Path.Combine(uploadFolder, fileName);

            try
            {
                await System.IO.File.WriteAllBytesAsync(
                    filePath,
                    bytes
                );
            }
            catch
            {
                DeleteUploadedPhoto(filePath);
                throw;
            }

            return (filePath, "/images/users/" + fileName);
        }

        private static void DeleteUploadedPhoto(string? filePath)
        {
            if (filePath != null &&
                System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}