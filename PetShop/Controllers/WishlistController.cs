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

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            var sessionId = GetOrCreateSessionId();

            var items = await _context.Wishlist
                .Include(w => w.Product).ThenInclude(p => p.Images)
                .Where(w => w.SessionId == sessionId)
                .ToListAsync();

            return View(items);
        }

        // GET: /Wishlist/GetCount
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var sessionId = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(sessionId))
                return Json(new { count = 0 });

            var count = await _context.Wishlist.CountAsync(w => w.SessionId == sessionId);
            return Json(new { count });
        }

        // POST: /Wishlist/Toggle
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
        {
            var sessionId = GetOrCreateSessionId();

            var existing = await _context.Wishlist
                .FirstOrDefaultAsync(w => w.SessionId == sessionId && w.ProductId == productId);

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
                    ProductId = productId
                });
                inWishlist = true;
            }

            await _context.SaveChangesAsync();

            var count = await _context.Wishlist.CountAsync(w => w.SessionId == sessionId);

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
        public async Task<IActionResult> Add(int productId, string? returnUrl = null)
        {
            var sessionId = GetOrCreateSessionId();

            var exists = await _context.Wishlist
                .AnyAsync(w => w.SessionId == sessionId && w.ProductId == productId);

            if (!exists)
            {
                _context.Wishlist.Add(new Wishlist
                {
                    SessionId = sessionId,
                    ProductId = productId
                });
                await _context.SaveChangesAsync();
            }

            var count = await _context.Wishlist.CountAsync(w => w.SessionId == sessionId);

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
        public async Task<IActionResult> Remove(int wishlistItemId, string? returnUrl = null)
        {
            var item = await _context.Wishlist.FindAsync(wishlistItemId);
            if (item != null)
            {
                _context.Wishlist.Remove(item);
                await _context.SaveChangesAsync();
            }

            var sessionId = GetOrCreateSessionId();
            var count = await _context.Wishlist.CountAsync(w => w.SessionId == sessionId);

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
