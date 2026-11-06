using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Freshify.ViewModels
{
    public class CheckoutViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(15)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(250)]
        [Display(Name = "Delivery Address")]
        public string AddressLine { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, StringLength(10)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [BindNever]
        public List<CartLineViewModel> Items { get; set; } = new();

        public decimal Subtotal =>
            Items.Sum(item => item.Subtotal);
    }
}