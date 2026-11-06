using Freshify.Data;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalCustomers = await _context.Users
                    .CountAsync(u => u.Role == "Customer"),

                TotalProducts = await _context.Products
                    .CountAsync(),

                TotalOrders = await _context.Orders
                    .CountAsync(),

                TotalSales = await _context.Orders
                    .Where(o => o.OrderStatus != "Cancelled")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0,

                PendingOrders = await _context.Orders
                    .CountAsync(o => o.OrderStatus == "Pending"),

                LowStockProducts = await _context.Products
                    .CountAsync(p => p.StockQuantity < 10),

                RecentOrders = await _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new RecentOrderViewModel
                    {
                        OrderId = o.OrderId,
                        CustomerName = o.User != null
                            ? o.User.FullName
                            : "Unknown",
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        OrderStatus = o.OrderStatus
                    })
                    .ToListAsync()
            };

            return View(model);
        }
    }
}