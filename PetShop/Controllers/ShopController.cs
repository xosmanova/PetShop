using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using PetShop.Models;

namespace PetShop.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private const int PageSize = 9;

        public ShopController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
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

            var cacheKey = $"Shop_Products_p{page}_c{category}_{categoryId}_t{tag}_b{brand}_{brandId}_min{minPrice}_max{maxPrice}_s{sort}";

            if (!_cache.TryGetValue(cacheKey, out (List<Product> products, int totalCount) cachedResult))
            {
                var query = _context.Products
                    .AsNoTracking()
                    .AsSplitQuery()
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
                var lowerCat = category.Trim().ToLower();
                if (lowerCat == "dogs" || lowerCat == "it")
                {
                    query = query.Where(p =>
                        p.ProductAnimalTypes.Any(pa => pa.AnimalType.Name.ToLower().Contains("it") || pa.AnimalType.Name.ToLower().Contains("dog")) ||
                        p.ProductCategory.Slug.Contains("dog") ||
                        p.Name.ToLower().Contains("dog"));
                }
                else if (lowerCat == "cats" || lowerCat == "pisik")
                {
                    query = query.Where(p =>
                        p.ProductAnimalTypes.Any(pa => pa.AnimalType.Name.ToLower().Contains("pisik") || pa.AnimalType.Name.ToLower().Contains("cat")) ||
                        p.ProductCategory.Slug.Contains("cat") ||
                        p.Name.ToLower().Contains("cat"));
                }
                else if (lowerCat == "birds" || lowerCat == "qus")
                {
                    query = query.Where(p =>
                        p.ProductAnimalTypes.Any(pa => pa.AnimalType.Name.ToLower().Contains("qus") || pa.AnimalType.Name.ToLower().Contains("bird")) ||
                        p.ProductCategory.Slug.Contains("bird") ||
                        p.Name.ToLower().Contains("bird"));
                }
                else
                {
                    query = query.Where(p =>
                        (p.ProductCategory != null && (p.ProductCategory.Slug.ToLower() == lowerCat || p.ProductCategory.Name.ToLower() == lowerCat)) ||
                        (p.ProductCategory != null && p.ProductCategory.ParentCategory != null && 
                            (p.ProductCategory.ParentCategory.Slug.ToLower() == lowerCat || p.ProductCategory.ParentCategory.Name.ToLower() == lowerCat)));
                }
            }
            else if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.ProductCategoryId == categoryId.Value ||
                    (p.ProductCategory != null && p.ProductCategory.ParentCategoryId == categoryId.Value));
            }

            // 2. Brand Filter
            if (!string.IsNullOrEmpty(brand) && !brand.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var lowerBrand = brand.Trim().ToLower();
                query = query.Where(p => p.Brand != null && p.Brand.Name.ToLower() == lowerBrand);
            }
            else if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            // 3. Price Filter
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
            }

            // 4. Sorting
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

                cachedResult = (products, totalCount);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(3))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                _cache.Set(cacheKey, cachedResult, cacheEntryOptions);
            }

            var (cachedProducts, cachedTotalCount) = cachedResult;
            var finalTotalPages = Math.Max(1, (int)Math.Ceiling(cachedTotalCount / (double)PageSize));

            // Kateqoriya və Brend siyahılarını keşləyirik (Sidebar filtrləri üçün)
            var categories = await _cache.GetOrCreateAsync("Shop_Categories_Hierarchy", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(15);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _context.ProductCategories
                    .AsNoTracking()
                    .Include(c => c.SubCategories)
                    .OrderBy(c => c.Id)
                    .ToListAsync();
            }) ?? new List<ProductCategory>();

            var brands = await _cache.GetOrCreateAsync("Shop_Real_Brands", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(15);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _context.Brands
                    .AsNoTracking()
                    .Where(b => b.Name != "Denim" && b.Name != "Puma" && b.Name != "Klaws" && b.Name != "Bio" && b.Name != "Arr")
                    .OrderBy(b => b.Name)
                    .ToListAsync();
            }) ?? new List<Brand>();

            var sessionId = HttpContext.Session.GetString("BasketSessionId");
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            var wishlistProductIds = (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(userId))
                ? new List<int>()
                : await _context.Wishlist
                    .AsNoTracking()
                    .Where(w => (!string.IsNullOrEmpty(userId) && w.UserId == userId) || (!string.IsNullOrEmpty(sessionId) && w.SessionId == sessionId && w.UserId == null))
                    .Select(w => w.ProductId)
                    .ToListAsync();

            var viewModel = new ShopViewModel
            {
                Products = cachedProducts,
                Categories = categories,
                Brands = brands,
                CurrentPage = page,
                TotalPages = finalTotalPages,
                TotalCount = cachedTotalCount,
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
                .AsNoTracking()
                .AsSplitQuery()
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
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Images)
                .Include(p => p.ProductCategory)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.ProductAnimalTypes)
                    .ThenInclude(pa => pa.AnimalType)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return NotFound();

            var reviews = product.Reviews.OrderByDescending(r => r.CreatedDate).ToList();
            int totalReviews = reviews.Count;
            double averageRating = totalReviews > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0;

            var ratingDistribution = new Dictionary<int, int>
            {
                { 5, reviews.Count(r => r.Rating == 5) },
                { 4, reviews.Count(r => r.Rating == 4) },
                { 3, reviews.Count(r => r.Rating == 3) },
                { 2, reviews.Count(r => r.Rating == 2) },
                { 1, reviews.Count(r => r.Rating == 1) }
            };

            bool isAuth = User.Identity?.IsAuthenticated == true;
            string? userId = isAuth ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            string? userEmail = isAuth ? User.FindFirstValue(ClaimTypes.Email) : null;

            bool hasPurchased = false;
            Review? currentUserReview = null;

            if (isAuth && !string.IsNullOrEmpty(userId))
            {
                hasPurchased = await _context.Orders
                    .AnyAsync(o => (o.UserId == userId || (!string.IsNullOrEmpty(userEmail) && o.Email == userEmail))
                                && o.Status != OrderStatus.Cancelled
                                && o.OrderItems.Any(oi => oi.ProductId == id));

                currentUserReview = reviews.FirstOrDefault(r => r.UserId == userId);
            }

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = reviews,
                AverageRating = averageRating,
                TotalReviews = totalReviews,
                RatingDistribution = ratingDistribution,
                IsUserAuthenticated = isAuth,
                HasPurchased = hasPurchased,
                HasReviewed = currentUserReview != null,
                CurrentUserReview = currentUserReview,
                NewReview = new ReviewInputModel
                {
                    ProductId = id,
                    Rating = currentUserReview?.Rating ?? 5,
                    Comment = currentUserReview?.Comment ?? string.Empty
                }
            };

            return View(viewModel);
        }

        // POST: /Shop/AddReview
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(ReviewInputModel model)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == model.ProductId && p.IsActive);
            if (product == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Sifariş üzərindən alıcı hüququnun yoxlanılması
            var hasPurchased = await _context.Orders
                .AnyAsync(o => (o.UserId == userId || (!string.IsNullOrEmpty(userEmail) && o.Email == userEmail))
                            && o.Status != OrderStatus.Cancelled
                            && o.OrderItems.Any(oi => oi.ProductId == model.ProductId));

            if (!hasPurchased)
            {
                TempData["ReviewError"] = "Yalnız bu məhsulu satın almış müştərilər rəy yaza bilər.";
                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            if (model.Rating < 1 || model.Rating > 5 || string.IsNullOrWhiteSpace(model.Comment))
            {
                TempData["ReviewError"] = "Zəhmət olmasa qiymətləndirmə ulduzunu (1-5) və rəy mətnini düzgün daxil edin.";
                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == model.ProductId && r.UserId == userId);

            if (existingReview != null)
            {
                existingReview.Rating = model.Rating;
                existingReview.Comment = model.Comment.Trim();
                existingReview.CreatedDate = DateTime.UtcNow;
                TempData["ReviewSuccess"] = "Rəyiniz uğurla yeniləndi!";
            }
            else
            {
                var newReview = new Review
                {
                    ProductId = model.ProductId,
                    UserId = userId,
                    Rating = model.Rating,
                    Comment = model.Comment.Trim(),
                    CreatedDate = DateTime.UtcNow
                };
                _context.Reviews.Add(newReview);
                TempData["ReviewSuccess"] = "Rəyiniz uğurla əlavə edildi! Təşəkkür edirik.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }
    }
}
