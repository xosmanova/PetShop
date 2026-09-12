using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.Controllers
{
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;
        private const string SessionKey = "BasketSessionId"; // basketlə eyni session-u paylaşırıq

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        private string GetOrCreateSessionId()
        {
            var sessionId = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString(SessionKey, sessionId);
            }
            return sessionId;
        }

        private string? GetCurrentUserId()
        {
            return User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        }

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            var sessionId = GetOrCreateSessionId();
            var userId = GetCurrentUserId();

            var items = await _context.Wishlist
                .Include(w => w.Product).ThenInclude(p => p.Images)
                .Where(w => (!string.IsNullOrEmpty(userId) && w.UserId == userId) || (w.SessionId == sessionId && w.UserId == null))
                .ToListAsync();

            return View(items);
        }

        // GET: /Wishlist/GetCount
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var sessionId = HttpContext.Session.GetString(SessionKey);
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(userId))
                return Json(new { count = 0 });

            var count = await _context.Wishlist
                .CountAsync(w => (!string.IsNullOrEmpty(userId) && w.UserId == userId) || (!string.IsNullOrEmpty(sessionId) && w.SessionId == sessionId && w.UserId == null));

            return Json(new { count });
        }

        // POST: /Wishlist/Toggle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
        {
            var sessionId = GetOrCreateSessionId();
            var userId = GetCurrentUserId();

            var existing = await _context.Wishlist
                .FirstOrDefaultAsync(w => ((!string.IsNullOrEmpty(userId) && w.UserId == userId) || (w.SessionId == sessionId && w.UserId == null)) && w.ProductId == productId);

            bool inWishlist;
            if (existing != null)
            {
                _context.Wishlist.Remove(existing);
                inWishlist = false;
            }
            else
            {
                _context.Wishlist.Add(new Wishlist
                {
                    SessionId = sessionId,
                    UserId = userId,
                    ProductId = productId
                });
                inWishlist = true;
            }

            await _context.SaveChangesAsync();

            var count = await _context.Wishlist
                .CountAsync(w => (!string.IsNullOrEmpty(userId) && w.UserId == userId) || (w.SessionId == sessionId && w.UserId == null));

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, inWishlist, count });
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Wishlist/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, string? returnUrl = null)
        {
            var sessionId = GetOrCreateSessionId();
            var userId = GetCurrentUserId();

            var exists = await _context.Wishlist
                .AnyAsync(w => ((!string.IsNullOrEmpty(userId) && w.UserId == userId) || (w.SessionId == sessionId && w.UserId == null)) && w.ProductId == productId);

            if (!exists)
            {
                _context.Wishlist.Add(new Wishlist
                {
                    SessionId = sessionId,
                    UserId = userId,
                    ProductId = productId
                });
                await _context.SaveChangesAsync();
            }

            var count = await _context.Wishlist
                .CountAsync(w => (!string.IsNullOrEmpty(userId) && w.UserId == userId) || (w.SessionId == sessionId && w.UserId == null));

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, inWishlist = true, count });
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Wishlist/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int wishlistItemId, string? returnUrl = null)
        {
            var item = await _context.Wishlist.FindAsync(wishlistItemId);
            if (item != null)
            {
                _context.Wishlist.Remove(item);
                await _context.SaveChangesAsync();
            }

            var sessionId = GetOrCreateSessionId();
            var userId = GetCurrentUserId();
            var count = await _context.Wishlist
                .CountAsync(w => (!string.IsNullOrEmpty(userId) && w.UserId == userId) || (w.SessionId == sessionId && w.UserId == null));

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, inWishlist = false, count });
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
