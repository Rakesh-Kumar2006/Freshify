using System;
using System.Collections.Generic;
using System.Linq;

namespace Freshify.ViewModels
{
    // SINGLE REVIEW DETAILS
    // Used in Index.cshtml and Details.cshtml
    public class AdminReviewRowViewModel
    {
        public int ReviewId { get; set; }

        // CUSTOMER DETAILS
        public string CustomerName { get; set; } = "";

        // PRODUCT DETAILS
        public string ProductName { get; set; } = "";

        // ORDER DETAILS
        public int? OrderId { get; set; }

        // RATING (1 TO 5)
        public int? Rating { get; set; }

        // REVIEW INFORMATION
        public string? ReviewTitle { get; set; }

        public string? Comment { get; set; }

        // REVIEW / FEEDBACK TYPE
        public string? FeedbackType { get; set; }

        // REVIEW DATE
        public DateTime CreatedAt { get; set; }
    }

    // REVIEW MANAGEMENT PAGE
    // Used in Index.cshtml
    public class AdminReviewsViewModel
    {
        // ALL CUSTOMER REVIEWS
        public List<AdminReviewRowViewModel> Reviews { get; set; }
            = new List<AdminReviewRowViewModel>();

        // TOTAL REVIEWS
        public int TotalReviews
        {
            get
            {
                return Reviews.Count;
            }
        }

        // TOTAL FIVE STAR REVIEWS
        public int FiveStarReviews
        {
            get
            {
                return Reviews.Count(r => r.Rating == 5);
            }
        }

        // AVERAGE CUSTOMER RATING
        public double AverageRating
        {
            get
            {
                var ratings = Reviews
                    .Where(r => r.Rating.HasValue)
                    .Select(r => (double)r.Rating!.Value)
                    .ToList();

                if (ratings.Count == 0)
                {
                    return 0;
                }

                return ratings.Average();
            }
        }
    }
}