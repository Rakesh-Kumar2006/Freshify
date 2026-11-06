using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Freshify.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        [Required, StringLength(30)]
        public string PaymentMethod { get; set; } = "CashOnDelivery";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime? PaymentDate { get; set; }

        public Order? Order { get; set; }
    }
}