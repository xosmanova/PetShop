namespace PetShop.Models
{
    public class BasketViewModel
    {
        public List<BasketItemDetailViewModel> Items { get; set; } = new();

        public decimal TotalPrice => Items.Sum(i => i.TotalPrice);
        public int TotalItemCount => Items.Sum(i => i.Quantity);
    }
}
