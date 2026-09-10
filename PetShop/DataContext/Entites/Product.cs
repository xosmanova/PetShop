using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace PetShop.DataContext.Entites
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? Sku { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ProductCategoryId { get; set; }
        public ProductCategory ProductCategory { get; set; } = null!;

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductAnimalType> ProductAnimalTypes { get; set; } = new List<ProductAnimalType>();
        public ICollection<BasketItem> BasketItems { get; set; } = new List<BasketItem>();
    }
    
}
