using System;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Freshify.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _logger = logger;
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

        private async Task<CheckoutViewModel> LoadCart(int userId)
        {
            var items = await (
                from cart in _context.Set<CartItem>().AsNoTracking()
                join product in _context.Products.AsNoTracking()
                    on cart.ProductId equals product.ProductId
                where cart.UserId == userId
                orderby product.Name
                select new CartLineViewModel
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl,
                    Unit = product.Unit,
                    Price = product.Price,
                    Quantity = cart.Quantity,
                    StockQuantity = product.StockQuantity
                }
            ).ToListAsync();

            return new CheckoutViewModel
            {
                Items = items
            };
        }

        // CHECKOUT PAGE
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId.Value &&
                    u.Role == "Customer");

            if (customer == null)
            {
                return NotFound();
            }

            var model = await LoadCart(userId.Value);

            if (model.Items.Count == 0 ||
                model.Items.Any(i =>
                    i.Quantity < 1 ||
                    i.Quantity > 1000 ||
                    i.Quantity > i.StockQuantity))
            {
                TempData["Error"] =
                    "Please review your cart quantities before checkout.";

                return RedirectToAction("Index", "Cart");
            }

            model.FullName = customer.FullName;
            model.Phone = customer.Phone ?? string.Empty;
            model.AddressLine = customer.AddressLine ?? string.Empty;
            model.City = customer.City ?? string.Empty;
            model.State = customer.State ?? string.Empty;
            model.PostalCode = customer.PostalCode ?? string.Empty;
            model.Country = customer.Country ?? string.Empty;

            return View(model);
        }

        // PLACE CASH ON DELIVERY ORDER
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            CheckoutViewModel model)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            // Do not trust product prices or cart items from the form.
            if (!ModelState.IsValid)
            {
                model.Items = (await LoadCart(userId.Value)).Items;
                return View("Index", model);
            }

            int orderId;

            // Serializable prevents concurrent checkout requests
            // from consuming the same cart or overselling stock.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable
                );

            try
            {
                var customerExists = await _context.Users
                    .AnyAsync(u =>
                        u.UserId == userId.Value &&
                        u.Role == "Customer");

                if (!customerExists)
                {
                    return NotFound();
                }

                var cartItems = await _context.Set<CartItem>()
                    .Where(c => c.UserId == userId.Value)
                    .OrderBy(c => c.ProductId)
                    .Include(c => c.Product)
                    .ToListAsync();

                if (cartItems.Count == 0)
                {
                    TempData["Error"] =
                        "Your cart is empty. An order may already have been placed.";

                    return RedirectToAction("Index", "Cart");
                }

                if (cartItems.Any(c =>
                    c.Product == null ||
                    c.Quantity < 1 ||
                    c.Quantity > 1000))
                {
                    TempData["Error"] =
                        "Your cart contains an invalid item. Please review it.";

                    return RedirectToAction("Index", "Cart");
                }

                var order = new Order
                {
                    UserId = userId.Value,
                    OrderDate = DateTime.UtcNow,
                    OrderStatus = "Pending",

                    DeliveryName = model.FullName.Trim(),
                    DeliveryPhone = model.Phone.Trim(),
                    DeliveryAddress = model.AddressLine.Trim(),
                    DeliveryCity = model.City.Trim(),
                    DeliveryState = model.State.Trim(),
                    DeliveryPostalCode = model.PostalCode.Trim(),
                    DeliveryCountry = model.Country.Trim()
                };

                decimal total = 0;

                // Combine any duplicate cart rows for the same product.
                foreach (var group in cartItems.GroupBy(c => c.ProductId))
                {
                    var product = group.First().Product!;
                    var quantity = group.Sum(c => (long)c.Quantity);

                    if (quantity > 1000 ||
                        quantity > product.StockQuantity ||
                        product.Price <= 0)
                    {
                        TempData["Error"] =
                            $"Please review the quantity or availability of {product.Name}.";

                        return RedirectToAction("Index", "Cart");
                    }

                    var orderedQuantity = (int)quantity;
                    var subtotal = product.Price * orderedQuantity;

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.ProductId,
                        Quantity = orderedQuantity,
                        UnitPrice = product.Price,
                        Subtotal = subtotal
                    });

                    product.StockQuantity -= orderedQuantity;
                    total += subtotal;
                }

                // No delivery charge is added in this version.
                order.TotalAmount = total;

                order.Payments.Add(new Payment
                {
                    PaymentMethod = "CashOnDelivery",
                    Amount = total,
                    PaymentStatus = "Pending",

                    // COD has not been collected yet.
                    PaymentDate = null
                });

                _context.Set<Order>().Add(order);
                _context.Set<CartItem>().RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                orderId = order.OrderId;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    exception,
                    "COD checkout failed for customer {CustomerId}.",
                    userId.Value
                );

                TempData["Error"] =
                    "The order could not be placed. Please review your cart and try again.";

                return RedirectToAction("Index", "Cart");
            }

            return RedirectToAction(
                "Confirmation",
                new { id = orderId }
            );
        }

        // ORDER CONFIRMATION
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            var order = await _context.Set<Order>()
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.UserId == userId.Value);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}