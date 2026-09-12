using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;
using PetShop.Models;

namespace PetShop.Controllers
{
    public class OffersController : Controller
    {
        private readonly AppDbContext _context;

        public OffersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Offers
        public async Task<IActionResult> Index(string? sort = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p => p.IsActive && p.DiscountPrice.HasValue && p.DiscountPrice.Value < p.Price)
                .Include(p => p.Images)
                .Include(p => p.ProductCategory)
                .Include(p => p.Brand)
                .AsQueryable();

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                "discount_desc" => query.OrderByDescending(p => p.Price - (p.DiscountPrice ?? p.Price)),
                "name_asc" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.Price - (p.DiscountPrice ?? p.Price))
            };

            var products = await query.ToListAsync();

            var sessionId = HttpContext.Session.GetString("BasketSessionId");
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            var wishlistProductIds = (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(userId))
                ? new List<int>()
                : await _context.Wishlist
                    .AsNoTracking()
                    .Where(w => (!string.IsNullOrEmpty(userId) && w.UserId == userId) || (!string.IsNullOrEmpty(sessionId) && w.SessionId == sessionId && w.UserId == null))
                    .Select(w => w.ProductId)
                    .ToListAsync();

            var viewModel = new OffersViewModel
            {
                Products = products,
                WishlistProductIds = wishlistProductIds,
                Sort = sort
            };

            return View(viewModel);
        }
    }
}
