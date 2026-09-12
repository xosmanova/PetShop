using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Models
{
    public class BlogPostFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Məqalə başlığı mütləq daxil edilməlidir.")]
        [StringLength(250, ErrorMessage = "Başlıq 250 simvoldan artıq ola bilməz.")]
        [Display(Name = "Məqalə Başlığı")]
        public string Title { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Slug 250 simvoldan artıq ola bilməz.")]
        [Display(Name = "URL Slug")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "Qısa təsvir (xülasə) mütləq daxil edilməlidir.")]
        [StringLength(500, ErrorMessage = "Qısa xülasə 500 simvoldan artıq ola bilməz.")]
        [Display(Name = "Qısa Xülasə")]
        public string Summary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Məqalə mətni mütləq daxil edilməlidir.")]
        [Display(Name = "Məqalə Məzmunu")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Müəllif adı daxil edilməlidir.")]
        [StringLength(100, ErrorMessage = "Müəllif adı 100 simvoldan artıq ola bilməz.")]
        [Display(Name = "Müəllif")]
        public string AuthorName { get; set; } = "Admin";

        public string? ExistingImageUrl { get; set; }

        [Display(Name = "Məqalə Şəkli")]
        public IFormFile? ImageFile { get; set; }
    }

    public class AdminBlogListViewModel
    {
        public List<BlogPost> Posts { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public string? Search { get; set; }
    }
}
