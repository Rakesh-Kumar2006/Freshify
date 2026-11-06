using Freshify.Models;

namespace Freshify.ViewModels
{
    public class AdminOrdersViewModel
    {
        public List<Order> Orders { get; set; }
            = new List<Order>();

        public int TotalOrders { get; set; }

        public int PendingOrders { get; set; }

        public int DeliveredOrders { get; set; }

        public int CancelledOrders { get; set; }
    }
}