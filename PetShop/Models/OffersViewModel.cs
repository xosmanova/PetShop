using PetShop.DataContext.Entites;

namespace PetShop.Models
{
    public class OffersViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<int> WishlistProductIds { get; set; } = new();
        public string? Sort { get; set; }
        public int TotalCount => Products.Count;
    }
}
