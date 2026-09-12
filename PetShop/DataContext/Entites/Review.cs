using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetShop.DataContext.Entites
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required(ErrorMessage = "Zəhmət olmasa qiymət (1-5 ulduz) verin.")]
        [Range(1, 5, ErrorMessage = "Qiymətləndirmə 1 və 5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Rəy mətni daxil edilməlidir.")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "Rəy 3-1000 simvol aralığında olmalıdır.")]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
