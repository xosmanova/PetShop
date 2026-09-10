using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.Models;

namespace PetShop.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 9;

        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            string? category = null,
            int? categoryId = null,
            string? tag = null,
            string? brand = null,
            int? brandId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sort = null)
        {
            if (page < 1) page = 1;

            var query = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Images)
                .Include(p => p.ProductCategory)
                .Include(p => p.Brand)
                .Include(p => p.ProductAnimalTypes)
                    .ThenInclude(pa => pa.AnimalType)
                .AsQueryable();

            // 1. Category Filter
            if (!string.IsNullOrEmpty(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var lowerCat = category.ToLower();
                if (lowerCat == "dogs")
                {
                    query = query.Where(p =>
                        p.ProductAnimalTypes.Any(pa => pa.AnimalType.Name.ToLower().Contains("it") || pa.AnimalType.Name.ToLower().Contains("dog")) ||
                        p.ProductCategory.Slug.Contains("dog") ||
                        p.Name.ToLower().Contains("dog"));
                }
                else if (lowerCat == "food")
                {
                    query = query.Where(p =>
                        p.ProductCategory.Slug.StartsWith("food") ||
                        p.ProductCategory.Name.ToLower().Contains("yem") ||
                        p.Name.ToLower().Contains("food"));
                }
                else if (lowerCat == "cats")
                {
                    query = query.Where(p =>
                        p.ProductAnimalTypes.Any(pa => pa.AnimalType.Name.ToLower().Contains("pisik") || pa.AnimalType.Name.ToLower().Contains("cat")) ||
                        p.ProductCategory.Slug.Contains("cat") ||
                        p.Name.ToLower().Contains("cat"));
                }
                else if (lowerCat == "birds")
                {
                    query = query.Where(p =>
                        p.ProductAnimalTypes.Any(pa => pa.AnimalType.Name.ToLower().Contains("qus") || pa.AnimalType.Name.ToLower().Contains("bird")) ||
                        p.ProductCategory.Slug.Contains("bird") ||
                        p.Name.ToLower().Contains("bird"));
                }
                else
                {
                    query = query.Where(p => p.ProductCategory.Name.ToLower() == lowerCat || p.ProductCategory.Slug == lowerCat);
                }
            }
            else if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.ProductCategoryId == categoryId.Value ||
                    p.ProductCategory.ParentCategoryId == categoryId.Value);
            }

            // 2. Tag Filter
            if (!string.IsNullOrEmpty(tag))
            {
                var lowerTag = tag.ToLower();
                if (lowerTag == "clothes")
                {
                    query = query.Where(p =>
                        p.ProductCategory.Slug.StartsWith("cloth") ||
                        p.ProductCategory.Name.ToLower().Contains("geyim") ||
                        p.Name.ToLower().Contains("jacket") ||
                        p.Name.ToLower().Contains("hoodie") ||
                        p.Name.ToLower().Contains("cloth"));
                }
                else if (lowerTag == "foods")
                {
                    query = query.Where(p =>
                        p.ProductCategory.Slug.StartsWith("food") ||
                        p.ProductCategory.Name.ToLower().Contains("yem") ||
                        p.Name.ToLower().Contains("food"));
                }
                else if (lowerTag == "toys")
                {
                    query = query.Where(p =>
                        p.ProductCategory.Slug.Contains("toy") ||
                        p.ProductCategory.Name.ToLower().Contains("oyuncaq") ||
                        p.Name.ToLower().Contains("toy"));
                }
                else if (lowerTag == "pets")
                {
                    // Pets represents general pet supplies
                    query = query.Where(p => p.ProductAnimalTypes.Any() || p.ProductCategory != null);
                }
            }


            // 3. Brand Filter
            if (!string.IsNullOrEmpty(brand))
            {
                var lowerBrand = brand.ToLower();
                query = query.Where(p => p.Brand != null && p.Brand.Name.ToLower() == lowerBrand);
            }
            else if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            // 4. Price Filter
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
            }

            // 5. Sorting
            query = sort switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                _ => query.OrderBy(p => p.Id)
            };

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            if (page > totalPages) page = totalPages;

            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var categories = await _context.ProductCategories
                .Where(c => c.ParentCategoryId == null)
                .ToListAsync();

            var brands = await _context.Brands.ToListAsync();

            var sessionId = HttpContext.Session.GetString("BasketSessionId");
            var wishlistProductIds = string.IsNullOrEmpty(sessionId)
                ? new List<int>()
                : await _context.Wishlist
                    .Where(w => w.SessionId == sessionId)
                    .Select(w => w.ProductId)
                    .ToListAsync();

            var viewModel = new ShopViewModel
            {
                Products = products,
                Categories = categories,
                Brands = brands,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                SelectedCategory = category,
                SelectedCategoryId = categoryId,
                SelectedTag = tag,
                SelectedBrand = brand,
                SelectedBrandId = brandId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Sort = sort,
                WishlistProductIds = wishlistProductIds
            };

            return View(viewModel);
        }
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }

            var products = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.ProductCategory)
                .Where(p => p.IsActive && p.Name.Contains(query))
                .ToListAsync();

            ViewBag.SearchQuery = query;
            return View("SearchResults", products);
        }
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.ProductCategory)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.ProductAnimalTypes)
                    .ThenInclude(pa => pa.AnimalType)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}
