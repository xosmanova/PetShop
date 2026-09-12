using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetShop.DataContext.Entites
{
    public enum DiscountType
    {
        Percentage = 1, // Faizlə endirim (%)
        Fixed = 2       // Sabit məbləğlə endirim (₼)
    }

    public class Coupon
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kupon kodu daxil edilməlidir")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kupon kodu 3-50 simvol aralığında olmalıdır")]
        [Display(Name = "Kupon Kodu")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Endirim Növü")]
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Required(ErrorMessage = "Endirim dəyəri daxil edilməlidir")]
        [Range(0.01, 100000.0, ErrorMessage = "Endirim dəyəri 0-dan böyük olmalıdır")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Endirim Dəyəri")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Bitmə Tarixi")]
        public DateTime? ExpiryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Minimum Sifariş Məbləği")]
        [Range(0.0, 100000.0, ErrorMessage = "Minimum sifariş məbləği mənfi ola bilməz")]
        public decimal? MinOrderAmount { get; set; }

        [Display(Name = "Aktivdir")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Yaradılma Tarixi")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
