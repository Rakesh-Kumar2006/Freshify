namespace Freshify.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }

        public List<RecentOrderViewModel> RecentOrders { get; set; }
            = new List<RecentOrderViewModel>();
    }

    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = "";
    }
}
