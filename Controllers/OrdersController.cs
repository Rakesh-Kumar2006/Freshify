using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
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

        // MY ORDERS
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var customerId = GetCustomerId();

            if (!customerId.HasValue)
            {
                return AccountRequired();
            }

            var orders = await _context.Set<Order>()
                .AsNoTracking()
                .Where(o => o.UserId == customerId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderId)
                .ToListAsync();

            return View(orders);
        }

        // ORDER DETAILS
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var customerId = GetCustomerId();

            if (!customerId.HasValue)
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
                    o.UserId == customerId.Value);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}