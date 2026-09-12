using Microsoft.AspNetCore.Http;
using PetShop.DataContext.Entites;
using System.ComponentModel.DataAnnotations;

namespace PetShop.Areas.Admin.Models
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;
        public string? SearchQuery { get; set; }
    }

    public class ProductVariantInputModel
    {
        public int Id { get; set; }
        public string? Size { get; set; }
        public string? Weight { get; set; }
        public string? Color { get; set; }
        public decimal PriceModifier { get; set; }
        public int StockQuantity { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Məhsul adı daxil edilməlidir.")]
        [StringLength(200, ErrorMessage = "Məhsul adı 200 simvoldan çox ola bilməz.")]
        [Display(Name = "Məhsul Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Təsvir")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Qiymət daxil edilməlidir.")]
        [Range(0.01, 999999.99, ErrorMessage = "Qiymət 0.01 ilə 999,999.99 arasında olmalıdır.")]
        [Display(Name = "Qiymət (₼)")]
        public decimal Price { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Endirimli qiymət 0 ilə 999,999.99 arasında olmalıdır.")]
        [Display(Name = "Endirimli Qiymət (₼)")]
        public decimal? DiscountPrice { get; set; }

        [Range(0, 100000, ErrorMessage = "Stok miqdarı mənfi ola bilməz.")]
        [Display(Name = "Stok Sayı")]
        public int StockQuantity { get; set; }

        [Display(Name = "SKU Kodu")]
        public string? Sku { get; set; }

        [Display(Name = "Aktivdir")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Kateqoriya seçilməlidir.")]
        [Display(Name = "Kateqoriya")]
        public int ProductCategoryId { get; set; }

        [Display(Name = "Brend")]
        public int? BrandId { get; set; }

        [Display(Name = "Şəkil")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }

        public List<ProductVariantInputModel> Variants { get; set; } = new();
    }
}
