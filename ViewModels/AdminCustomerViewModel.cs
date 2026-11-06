using Freshify.Models;
using System.Collections.Generic;

namespace Freshify.ViewModels
{
    public class AdminCustomersViewModel
    {
        public List<User> Customers { get; set; }
            = new List<User>();

        public int TotalCustomers => Customers.Count;
    }
}