using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using PetShop.Models;
using System.Diagnostics;

namespace PetShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories
                .Where(c => c.ParentCategoryId == null) // yaln?z ana kateqoriyalar
                .ToListAsync();

            var animalTypes = await _context.AnimalTypes.ToListAsync();

            var products = await _context.Products
                 .Include(p => p.Brand)
                            .Include(p => p.ProductCategory)
                            .Include(p => p.Images)
                            .Include(p => p.ProductAnimalTypes)
                            .ThenInclude(pat => pat.AnimalType)
                            .ToListAsync();

            var brands = await _context.Brands.ToListAsync();

            var banners = new List<BannerSliderViewModel>
        {
            new() { ImageUrl = "/images/banner-img.png", DiscountText = "Save 10 - 20 % off", HighlightedText = "your pets" },
            new() { ImageUrl = "/images/banner-img3.png", DiscountText = "Save 10 - 20 % off", HighlightedText = "your pets" },
            new() { ImageUrl = "/images/banner-img4.png", DiscountText = "Save 10 - 20 % off", HighlightedText = "your pets" }
        };

            var testimonials = new List<TestimonialViewModel>
        {
            new() { Content = "At the core of our practice is the idea that cities are the incubators of our greatest achievements, and the best hope for a sustainable future.", Author = "Joshima Lin" },
            new() { Content = "At the core of our practice is the idea that cities are the incubators of our greatest achievements, and the best hope for a sustainable future.", Author = "Joshima Lin" },
            new() { Content = "At the core of our practice is the idea that cities are the incubators of our greatest achievements, and the best hope for a sustainable future.", Author = "Joshima Lin" }
        };

            var viewModel = new HomeViewModel
            {
                Categories = categories,
                AnimalTypes = animalTypes,
                Products = products,
                Brands = brands,
                BannerSliders= banners,
                Testimonials = testimonials
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
