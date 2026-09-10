using PetShop.DataContext.Entites;

namespace PetShop.Models
{
    public class ShopViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<ProductCategory> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }

        public int? SelectedCategoryId { get; set; }
        public string? SelectedCategory { get; set; }
        public string? SelectedTag { get; set; }
        public string? SelectedBrand { get; set; }
        public int? SelectedBrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Sort { get; set; }

        public List<int> WishlistProductIds { get; set; } = new();
    }
}