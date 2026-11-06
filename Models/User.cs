using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Freshify.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Role { get; set; } = "Customer";

        // Profile image
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        // Default delivery address
        [StringLength(250)]
        public string? AddressLine { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        // Navigation properties
        public ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();

        public ICollection<Order> Orders { get; set; }
            = new List<Order>();
    }
}