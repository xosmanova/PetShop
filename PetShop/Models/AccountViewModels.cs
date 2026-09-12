using System.ComponentModel.DataAnnotations;
using PetShop.DataContext.Entites;

namespace PetShop.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad daxil edilməlidir")]
        [Display(Name = "Ad")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad daxil edilməlidir")]
        [Display(Name = "Soyad")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-poçt ünvanı daxil edilməlidir")]
        [EmailAddress(ErrorMessage = "Düzgün e-poçt ünvanı daxil edin")]
        [Display(Name = "E-poçt")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Düzgün telefon nömrəsi daxil edin")]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Şifrə daxil edilməlidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifrə minimum 6 simvol olmalıdır")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifrə")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifrə təkrarı")]
        [Compare("Password", ErrorMessage = "Şifrələr uyğun gəlmir")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-poçt ünvanı daxil edilməlidir")]
        [EmailAddress(ErrorMessage = "Düzgün e-poçt ünvanı daxil edin")]
        [Display(Name = "E-poçt")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifrə daxil edilməlidir")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifrə")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Məni xatırla")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Ad daxil edilməlidir")]
        [Display(Name = "Ad")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad daxil edilməlidir")]
        [Display(Name = "Soyad")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-poçt ünvanı daxil edilməlidir")]
        [EmailAddress(ErrorMessage = "Düzgün e-poçt ünvanı daxil edin")]
        [Display(Name = "E-poçt")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Düzgün telefon nömrəsi daxil edin")]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Ünvan")]
        [StringLength(200, ErrorMessage = "Ünvan maksimum 200 simvol ola bilər")]
        public string? Address { get; set; }

        [Display(Name = "Şəhər")]
        [StringLength(100, ErrorMessage = "Şəhər maksimum 100 simvol ola bilər")]
        public string? City { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Cari şifrə")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Yeni şifrə minimum 6 simvol olmalıdır")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifrə")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifrə təkrarı")]
        [Compare("NewPassword", ErrorMessage = "Yeni şifrələr uyğun gəlmir")]
        public string? ConfirmNewPassword { get; set; }

        public List<Order> Orders { get; set; } = new();
        public string? StatusMessage { get; set; }
    }
}
