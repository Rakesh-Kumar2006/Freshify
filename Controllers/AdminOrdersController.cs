using System;
using System.Linq;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.Models;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DISPLAY ALL ORDERS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderId)
                .ToListAsync();

            var model = new AdminOrdersViewModel
            {
                Orders = orders,

                TotalOrders = orders.Count,

                PendingOrders = orders.Count(o =>
                    o.OrderStatus == "Pending"),

                DeliveredOrders = orders.Count(o =>
                    o.OrderStatus == "Delivered"),

                CancelledOrders = orders.Count(o =>
                    o.OrderStatus == "Cancelled")
            };

            return View(model);
        }

        // UPDATE ORDER STATUS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string? status)
        {
            var allowedStatuses = new[]
            {
                "Pending",
                "Confirmed",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled"
            };

            if (!ModelState.IsValid ||
                id <= 0 ||
                string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid order status.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            if (order.OrderStatus == "Cancelled" ||
                order.OrderStatus == "Delivered")
            {
                TempData["Error"] =
                    "Completed or cancelled orders cannot be changed.";

                return RedirectToAction(nameof(Index));
            }

            order.OrderStatus = status;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Order status updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // UPDATE PAYMENT STATUS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(
            int orderId,
            int paymentId,
            string? paymentStatus)
        {
            if (!ModelState.IsValid ||
                orderId <= 0 ||
                paymentId <= 0 ||
                (paymentStatus != "Pending" &&
                 paymentStatus != "Paid"))
            {
                TempData["Error"] =
                    "Select Pending or Paid as the payment status.";

                return RedirectToAction(nameof(Index));
            }

            // Verify that the payment belongs to the supplied order.
            var payment = await _context.Set<Payment>()
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == paymentId &&
                    p.OrderId == orderId);

            if (payment == null)
            {
                TempData["Error"] =
                    "Payment record not found for this order.";

                return RedirectToAction(nameof(Index));
            }

            if (paymentStatus == "Paid")
            {
                // Preserve the date when an already-paid record is saved.
                if (payment.PaymentStatus != "Paid" ||
                    !payment.PaymentDate.HasValue)
                {
                    payment.PaymentDate = DateTime.UtcNow;
                }
            }
            else
            {
                // Pending means payment has not been collected.
                payment.PaymentDate = null;
            }

            payment.PaymentStatus = paymentStatus;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Payment status for Order #{orderId} updated to {paymentStatus}.";

            return RedirectToAction(nameof(Index));
        }
    }
}