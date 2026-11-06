using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
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

            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier
            )?.Value;

            return int.TryParse(claim, out var id)
                ? id
                : null;
        }

        private IActionResult AccountRequired()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return StatusCode(403);
            }

            return RedirectToAction(
                "Login",
                "CustomerAccount"
            );
        }

        // CART PAGE
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            var items = await (
                from cart in _context.Set<CartItem>().AsNoTracking()
                join product in _context.Products.AsNoTracking()
                    on cart.ProductId equals product.ProductId
                where cart.UserId == userId.Value
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

            return View(new CartViewModel
            {
                Items = items
            });
        }

        // ADD PRODUCT TO CART
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            int productId,
            int quantity = 1)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            if (!ModelState.IsValid ||
                quantity < 1 ||
                quantity > 1000)
            {
                TempData["Error"] =
                    "Enter a quantity between 1 and 1000.";

                return RedirectToAction("Index");
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.ProductId == productId
                );

            if (product == null)
            {
                return NotFound();
            }

            var cartItem = await _context.Set<CartItem>()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId.Value &&
                    c.ProductId == productId
                );

            var existingQuantity = cartItem?.Quantity ?? 0;

            if (product.StockQuantity <= 0 ||
                quantity > product.StockQuantity ||
                existingQuantity > product.StockQuantity - quantity)
            {
                TempData["Error"] =
                    "The requested quantity exceeds available stock.";

                return RedirectToAction("Index");
            }

            if (existingQuantity > 1000 - quantity)
            {
                TempData["Error"] =
                    "You can add a maximum of 1000 units per product.";

                return RedirectToAction("Index");
            }

            if (cartItem == null)
            {
                _context.Set<CartItem>().Add(new CartItem
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity
                });
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product added to your cart.";

            return RedirectToAction("Index");
        }

        // BUY NOW: SAVE SELECTED PRODUCT AND OPEN CHECKOUT
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(
            int productId,
            int quantity = 1)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            if (!ModelState.IsValid ||
                quantity < 1 ||
                quantity > 1000)
            {
                TempData["Error"] =
                    "Enter a quantity between 1 and 1000.";

                return RedirectToAction("Index");
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.ProductId == productId
                );

            if (product == null)
            {
                return NotFound();
            }

            if (product.StockQuantity < quantity)
            {
                TempData["Error"] =
                    "The requested quantity is not available.";

                return RedirectToAction("Index");
            }

            var cartItem = await _context.Set<CartItem>()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId.Value &&
                    c.ProductId == productId
                );

            if (cartItem == null)
            {
                _context.Set<CartItem>().Add(new CartItem
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity
                });
            }
            else
            {
                // Set the selected quantity instead of adding it again.
                cartItem.Quantity = quantity;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Checkout");
        }

        // UPDATE QUANTITY
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            int productId,
            int quantity)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            if (!ModelState.IsValid ||
                quantity < 1 ||
                quantity > 1000)
            {
                TempData["Error"] =
                    "Enter a quantity between 1 and 1000.";

                return RedirectToAction("Index");
            }

            var cartItem = await _context.Set<CartItem>()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId.Value &&
                    c.ProductId == productId
                );

            if (cartItem == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.ProductId == productId
                );

            if (product == null)
            {
                return NotFound();
            }

            if (quantity > product.StockQuantity)
            {
                TempData["Error"] =
                    "The requested quantity exceeds available stock.";

                return RedirectToAction("Index");
            }

            cartItem.Quantity = quantity;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cart updated.";

            return RedirectToAction("Index");
        }

        // REMOVE PRODUCT
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = GetCustomerId();

            if (!userId.HasValue)
            {
                return AccountRequired();
            }

            var cartItem = await _context.Set<CartItem>()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId.Value &&
                    c.ProductId == productId
                );

            if (cartItem == null)
            {
                return NotFound();
            }

            _context.Set<CartItem>().Remove(cartItem);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product removed from your cart.";

            return RedirectToAction("Index");
        }
    }
}