using PetShop.DataContext.Entites;

namespace PetShop.Models
{
    public class HomeViewModel
    {
        public List<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
        public List<AnimalType> AnimalTypes { get; set; } = new List<AnimalType>();
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Brand> Brands { get; set; } = new List<Brand>();
        public List<BannerSliderViewModel> BannerSliders { get; set; } = new List<BannerSliderViewModel>();
        public List<TestimonialViewModel> Testimonials { get; set; } = new List<TestimonialViewModel>();
    }
}
