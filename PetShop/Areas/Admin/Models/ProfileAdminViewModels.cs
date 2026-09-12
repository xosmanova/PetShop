using System.ComponentModel.DataAnnotations;

namespace PetShop.Areas.Admin.Models
{
    public class AdminProfileViewModel
    {
        [Required(ErrorMessage = "Ad mütləq daxil edilməlidir.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad mütləq daxil edilməlidir.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-poçt mütləq daxil edilməlidir.")]
        [EmailAddress(ErrorMessage = "Düzgün e-poçt ünvanı daxil edin.")]
        [Display(Name = "E-poçt")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [Display(Name = "Şöbə")]
        public string? Department { get; set; }

        [Display(Name = "Vəzifə")]
        public string? JobTitle { get; set; }

        [Display(Name = "Saat Qurşağı")]
        public string? TimeZone { get; set; }

        // Read-only display helpers
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string Initials
        {
            get
            {
                var f = !string.IsNullOrWhiteSpace(FirstName) ? FirstName[0].ToString().ToUpper() : "";
                var l = !string.IsNullOrWhiteSpace(LastName) ? LastName[0].ToString().ToUpper() : "";
                var res = $"{f}{l}";
                return string.IsNullOrEmpty(res) ? "AD" : res;
            }
        }

        public List<string> Roles { get; set; } = new();
        public bool IsEmailConfirmed { get; set; }
    }
}
