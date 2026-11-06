using Freshify.Data;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCustomersController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // DISPLAY ALL CUSTOMERS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "Customer")
                .OrderByDescending(u => u.UserId)
                .ToListAsync();

            var model = new AdminCustomersViewModel
            {
                Customers = customers
            };

            return View(model);
        }

        // VIEW CUSTOMER DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Customer");

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }
    }
}