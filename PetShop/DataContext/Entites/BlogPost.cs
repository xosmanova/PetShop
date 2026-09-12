using System.ComponentModel.DataAnnotations;

namespace PetShop.DataContext.Entites
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlıq daxil edilməlidir.")]
        [StringLength(250, ErrorMessage = "Başlıq 250 simvoldan çox ola bilməz.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slug daxil edilməlidir.")]
        [StringLength(250, ErrorMessage = "Slug 250 simvoldan çox ola bilməz.")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Qısa xülasə daxil edilməlidir.")]
        [StringLength(500, ErrorMessage = "Qısa xülasə 500 simvoldan çox ola bilməz.")]
        public string Summary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Məqalə mətni daxil edilməlidir.")]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Müəllif adı daxil edilməlidir.")]
        [StringLength(100, ErrorMessage = "Müəllif adı 100 simvoldan çox ola bilməz.")]
        public string AuthorName { get; set; } = "Admin";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
