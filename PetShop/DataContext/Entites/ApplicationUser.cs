using Microsoft.AspNetCore.Identity;

namespace PetShop.DataContext.Entites
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Bio { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? TimeZone { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
