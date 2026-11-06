using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Freshify.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public int UserId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required, StringLength(30)]
        public string OrderStatus { get; set; } = "Pending";

        // Delivery address saved with this order
        [Required, StringLength(250)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string DeliveryCity { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string DeliveryState { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string DeliveryPostalCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string DeliveryCountry { get; set; } = "India";

        [StringLength(100)]
        public string DeliveryName { get; set; } = string.Empty;

        [StringLength(15)]
        public string DeliveryPhone { get; set; } = string.Empty;

        // Navigation properties
        public User? User { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}