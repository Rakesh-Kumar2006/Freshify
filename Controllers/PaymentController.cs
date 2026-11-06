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
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(
                    "Login",
                    "CustomerAccount"
                );
            }

            if (!User.IsInRole("Customer"))
            {
                return StatusCode(403);
            }

            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier
            )?.Value;

            if (!int.TryParse(claim, out var userId))
            {
                return StatusCode(403);
            }

            // Show only payments belonging to this customer.
            var payments = await _context.Set<Payment>()
                .AsNoTracking()
                .Include(p => p.Order)
                .Where(p =>
                    p.Order != null &&
                    p.Order.UserId == userId)
                .OrderByDescending(p => p.PaymentId)
                .ToListAsync();

            return View(payments);
        }
    }
}