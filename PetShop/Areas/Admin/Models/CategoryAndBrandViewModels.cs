using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PetShop.Areas.Admin.Models
{
    public class CategoryFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kateqoriya adı daxil edilməlidir.")]
        [StringLength(100, ErrorMessage = "Kateqoriya adı 100 simvoldan çox ola bilməz.")]
        [Display(Name = "Kateqoriya Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Slug (URL dostu identifikator)")]
        [StringLength(100, ErrorMessage = "Slug 100 simvoldan çox ola bilməz.")]
        public string? Slug { get; set; }

        [Display(Name = "İkon Sinfi (Məs: ph:bowl-food, ph:dog, bi bi-tag)")]
        [StringLength(100, ErrorMessage = "İkon sinfi 100 simvoldan çox ola bilməz.")]
        public string? IconClass { get; set; }

        [Display(Name = "Ana Kateqoriya")]
        public int? ParentCategoryId { get; set; }
    }

    public class BrandFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Brend adı daxil edilməlidir.")]
        [StringLength(100, ErrorMessage = "Brend adı 100 simvoldan çox ola bilməz.")]
        [Display(Name = "Brend Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Loqo Faylı")]
        public IFormFile? LogoFile { get; set; }

        public string? ExistingLogoUrl { get; set; }
    }
}
