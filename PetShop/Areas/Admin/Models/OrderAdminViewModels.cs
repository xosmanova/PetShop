using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Models
{
    public class OrderListAdminViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public OrderStatus? SelectedStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }

    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public decimal TodaySales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveProductsCount { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public decimal[] MonthlySales { get; set; } = new decimal[12];
    }
}
