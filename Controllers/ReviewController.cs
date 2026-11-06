using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCustomerId()
        {
            if (User.Identity?.IsAuthenticated != true ||
                !User.IsInRole("Customer"))
            {
                return null;
            }

            return int.TryParse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                out var id)
                ? id
                : null;
        }

        private IActionResult AccountRequired()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return StatusCode(403);
            }

            return RedirectToAction("Login", "CustomerAccount");
        }

        // LOAD PRODUCT, ORDER DROPDOWN, AND REVIEW HISTORY
        private async Task<bool> LoadPage(
            ReviewPageViewModel model,
            int userId)
        {
            if (model.ProductId.HasValue)
            {
                model.ProductName = await _context.Products
                    .AsNoTracking()
                    .Where(p =>
                        p.ProductId == model.ProductId.Value)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();

                if (model.ProductName == null)
                {
                    return false;
                }
            }

            // Only fetch this customer's orders.
            var ordersQuery = _context.Set<Order>()
                .AsNoTracking()
                .Where(o => o.UserId == userId);

            // Product reviews can reference only orders
            // containing that product.
            if (model.ProductId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.OrderItems.Any(i =>
                        i.ProductId == model.ProductId.Value));
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderId)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.TotalAmount
                })
                .ToListAsync();

            // Validate ownership and product/order relationship.
            if (model.OrderId.HasValue &&
                !orders.Any(o => o.OrderId == model.OrderId.Value))
            {
                return false;
            }

            model.OrderOptions = orders
                .Select(o => new SelectListItem
                {
                    Value = o.OrderId.ToString(),
                    Text = $"Order #{o.OrderId} — "
                        + $"{o.OrderDate:dd MMM yyyy} — "
                        + $"₹{o.TotalAmount:N2}"
                })
                .ToList();

            model.Reviews = await _context.Set<Review>()
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.ReviewId)
                .ToListAsync();

            return true;
        }

        // REVIEW PAGE
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            int? productId,
            int? orderId)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            var model = new ReviewPageViewModel
            {
                ProductId = productId,
                OrderId = orderId
            };

            if (!await LoadPage(model, userId.Value))
            {
                return NotFound();
            }

            return View(model);
        }

        // SAVE REVIEW
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ReviewPageViewModel model)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            // Reload dropdown options even when form validation fails.
            if (!await LoadPage(model, userId.Value))
            {
                return NotFound();
            }

            model.Comment = model.Comment?.Trim() ?? string.Empty;
            model.ReviewTitle = model.ReviewTitle?.Trim();

            if (string.IsNullOrWhiteSpace(model.Comment))
            {
                ModelState.AddModelError(
                    nameof(model.Comment),
                    "Please enter your feedback."
                );
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var review = new Review
            {
                UserId = userId.Value,
                ProductId = model.ProductId,
                OrderId = model.OrderId,
                Rating = model.Rating,
                ReviewTitle = model.ReviewTitle,
                Comment = model.Comment,

                FeedbackType = model.ProductId.HasValue
                    ? "Product"
                    : model.OrderId.HasValue
                        ? "Order"
                        : "General",

                CreatedAt = DateTime.UtcNow
            };

            _context.Set<Review>().Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Thank you! Your review has been submitted.";

            return RedirectToAction("Index");
        }
    }
}