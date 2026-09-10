using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.Controllers
{
    public class BasketController : Controller
    {
        private readonly AppDbContext _context;
        private const string SessionKey = "BasketSessionId";

        public BasketController(AppDbContext context)
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

        private async Task<Basket> GetOrCreateBasketAsync()
        {
            var sessionId = GetOrCreateSessionId();

            var basket = await _context.Baskets
                .Include(b => b.BasketItems).ThenInclude(bi => bi.Product).ThenInclude(p => p.Images)
                .Include(b => b.BasketItems).ThenInclude(bi => bi.ProductVariant)
                .FirstOrDefaultAsync(b => b.SessionId == sessionId);

            if (basket == null)
            {
                basket = new Basket { SessionId = sessionId };
                _context.Baskets.Add(basket);
                await _context.SaveChangesAsync();
            }

            return basket;
        }

        // GET: /Basket
        public async Task<IActionResult> Index()
        {
            var basket = await GetOrCreateBasketAsync();
            return View(basket);
        }

        // POST: /Basket/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int? productVariantId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var basket = await GetOrCreateBasketAsync();

            var existingItem = basket.BasketItems.FirstOrDefault(bi =>
                bi.ProductId == productId && bi.ProductVariantId == productVariantId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                basket.BasketItems.Add(new BasketItem
                {
                    BasketId = basket.Id,
                    ProductId = productId,
                    ProductVariantId = productVariantId,
                    Quantity = quantity,
                    UnitPrice = product.DiscountPrice ?? product.Price
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Basket/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int basketItemId, int quantity)
        {
            var item = await _context.BasketItems.FindAsync(basketItemId);
            if (item == null) return NotFound();

            if (quantity <= 0)
                _context.BasketItems.Remove(item);
            else
                item.Quantity = quantity;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Basket/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int basketItemId)
        {
            var item = await _context.BasketItems.FindAsync(basketItemId);
            if (item == null) return NotFound();

            _context.BasketItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}