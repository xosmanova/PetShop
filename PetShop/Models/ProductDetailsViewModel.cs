using System.ComponentModel.DataAnnotations;
using PetShop.DataContext.Entites;

namespace PetShop.Models
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // User review eligibility
        public bool IsUserAuthenticated { get; set; }
        public bool HasPurchased { get; set; }
        public bool HasReviewed { get; set; }
        public Review? CurrentUserReview { get; set; }

        // Distribution of stars: 5 -> count, 4 -> count, 3 -> count, etc.
        public Dictionary<int, int> RatingDistribution { get; set; } = new()
        {
            { 5, 0 },
            { 4, 0 },
            { 3, 0 },
            { 2, 0 },
            { 1, 0 }
        };

        public ReviewInputModel NewReview { get; set; } = new();
    }

    public class ReviewInputModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Zəhmət olmasa qiymətləndirmə ulduzunu (1-5) seçin.")]
        [Range(1, 5, ErrorMessage = "Qiymətləndirmə 1 və 5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Zəhmət olmasa rəyinizi qeyd edin.")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "Rəy 3 ilə 1000 simvol aralığında olmalıdır.")]
        public string Comment { get; set; } = string.Empty;
    }
}
