using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using PetShop.Models;

namespace PetShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private const string HomeCacheKey = "Home_Index_ViewModel";

        public HomeController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            if (!_cache.TryGetValue(HomeCacheKey, out HomeViewModel? viewModel) || viewModel == null)
            {
                var categories = await _context.ProductCategories
                    .AsNoTracking()
                    .Where(c => c.ParentCategoryId == null && !string.IsNullOrEmpty(c.IconClass) && c.Name != "Pişik Geyimi")
                    .ToListAsync();

                var animalTypes = await _context.AnimalTypes
                    .AsNoTracking()
                    .Where(a => a.Name != "Balıq" && a.Name != "Baliq")
                    .ToListAsync();

                var products = await _context.Products
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(p => p.IsActive)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductCategory)
                    .Include(p => p.Images)
                    .Include(p => p.ProductAnimalTypes)
                        .ThenInclude(pat => pat.AnimalType)
                    .ToListAsync();

                var brands = await _context.Brands
                    .AsNoTracking()
                    .ToListAsync();

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

                viewModel = new HomeViewModel
                {
                    Categories = categories,
                    AnimalTypes = animalTypes,
                    Products = products,
                    Brands = brands,
                    BannerSliders = banners,
                    Testimonials = testimonials
                };

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                _cache.Set(HomeCacheKey, viewModel, cacheOptions);
            }

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
