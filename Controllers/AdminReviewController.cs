using System.Linq;
using System.Threading.Tasks;
using Freshify.Data;
using Freshify.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshify.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReviewsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // DISPLAY ALL REVIEWS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var reviews = await (
                from r in _context.Reviews.AsNoTracking()

                join u in _context.Users.AsNoTracking()
                    on r.UserId equals u.UserId into users
                from u in users.DefaultIfEmpty()

                join p in _context.Products.AsNoTracking()
                    on r.ProductId equals (int?)p.ProductId
                    into products
                from p in products.DefaultIfEmpty()

                orderby r.CreatedAt descending

                select new AdminReviewRowViewModel
                {
                    ReviewId = r.ReviewId,

                    CustomerName = u != null
                        ? u.FullName
                        : "Unknown Customer",

                    ProductName = p != null
                        ? p.Name
                        : "General Feedback",

                    OrderId = r.OrderId,
                    Rating = r.Rating,
                    ReviewTitle = r.ReviewTitle,
                    Comment = r.Comment,
                    FeedbackType = r.FeedbackType,
                    CreatedAt = r.CreatedAt
                }
            ).ToListAsync();

            var model = new AdminReviewsViewModel
            {
                Reviews = reviews
            };

            return View(model);
        }

        // VIEW REVIEW DETAILS
        // GET: AdminReviews/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var review = await (
                from r in _context.Reviews.AsNoTracking()

                join u in _context.Users.AsNoTracking()
                    on r.UserId equals u.UserId into users
                from u in users.DefaultIfEmpty()

                join p in _context.Products.AsNoTracking()
                    on r.ProductId equals (int?)p.ProductId
                    into products
                from p in products.DefaultIfEmpty()

                where r.ReviewId == id

                select new AdminReviewRowViewModel
                {
                    ReviewId = r.ReviewId,

                    CustomerName = u != null
                        ? u.FullName
                        : "Unknown Customer",

                    ProductName = p != null
                        ? p.Name
                        : "General Feedback",

                    OrderId = r.OrderId,
                    Rating = r.Rating,
                    ReviewTitle = r.ReviewTitle,
                    Comment = r.Comment,
                    FeedbackType = r.FeedbackType,
                    CreatedAt = r.CreatedAt
                }
            ).FirstOrDefaultAsync();

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // DELETE REVIEW
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews
                .FindAsync(id);

            if (review == null)
            {
                TempData["Error"] =
                    "Review not found.";

                return RedirectToAction(nameof(Index));
            }

            _context.Reviews.Remove(review);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Review deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}