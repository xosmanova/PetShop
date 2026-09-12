namespace PetShop.DataContext.Entites
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public decimal TotalPrice { get; set; }
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? PaymentIntentId { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
