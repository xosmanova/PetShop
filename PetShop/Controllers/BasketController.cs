using System.Security.Claims;
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
        public const string CouponSessionKey = "AppliedCouponCode";

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
            string? userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            Basket? basket = null;
            if (!string.IsNullOrEmpty(userId))
            {
                basket = await _context.Baskets
                    .Include(b => b.BasketItems).ThenInclude(bi => bi.Product).ThenInclude(p => p.Images)
                    .Include(b => b.BasketItems).ThenInclude(bi => bi.ProductVariant)
                    .FirstOrDefaultAsync(b => b.UserId == userId);

                if (basket != null)
                {
                    if (basket.SessionId != sessionId)
                    {
                        basket.SessionId = sessionId;
                    }

                    // Qonaq sessiyasından qalan səbət varsa, istifadəçinin səbətinə birləşdir
                    var guestBasket = await _context.Baskets
                        .Include(b => b.BasketItems)
                        .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.UserId == null && b.Id != basket.Id);

                    if (guestBasket != null && guestBasket.BasketItems.Any())
                    {
                        foreach (var guestItem in guestBasket.BasketItems.ToList())
                        {
                            var existingItem = basket.BasketItems
                                .FirstOrDefault(bi => bi.ProductId == guestItem.ProductId && bi.ProductVariantId == guestItem.ProductVariantId);

                            if (existingItem != null)
                            {
                                existingItem.Quantity += guestItem.Quantity;
                            }
                            else
                            {
                                basket.BasketItems.Add(new BasketItem
                                {
                                    BasketId = basket.Id,
                                    ProductId = guestItem.ProductId,
                                    ProductVariantId = guestItem.ProductVariantId,
                                    Quantity = guestItem.Quantity,
                                    UnitPrice = guestItem.UnitPrice
                                });
                            }
                        }
                        _context.Baskets.Remove(guestBasket);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            if (basket == null)
            {
                basket = await _context.Baskets
                    .Include(b => b.BasketItems).ThenInclude(bi => bi.Product).ThenInclude(p => p.Images)
                    .Include(b => b.BasketItems).ThenInclude(bi => bi.ProductVariant)
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.UserId == null);
            }

            if (basket == null)
            {
                basket = new Basket { SessionId = sessionId, UserId = userId };
                _context.Baskets.Add(basket);
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrEmpty(userId) && basket.UserId == null)
            {
                basket.UserId = userId;
                await _context.SaveChangesAsync();
            }

            return basket;
        }

        private async Task SyncBasketItemPricesAsync(Basket basket)
        {
            bool hasChanges = false;
            foreach (var item in basket.BasketItems)
            {
                if (item.Product != null)
                {
                    var basePrice = (item.Product.DiscountPrice.HasValue && item.Product.DiscountPrice.Value < item.Product.Price)
                        ? item.Product.DiscountPrice.Value
                        : item.Product.Price;
                    var variantExtra = item.ProductVariant?.PriceModifier ?? 0m;
                    var activeUnitPrice = basePrice + variantExtra;

                    if (item.UnitPrice != activeUnitPrice)
                    {
                        item.UnitPrice = activeUnitPrice;
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        // GET: /Basket
        public async Task<IActionResult> Index()
        {
            var basket = await GetOrCreateBasketAsync();
            await SyncBasketItemPricesAsync(basket);
            var subTotal = basket.BasketItems.Sum(bi => bi.Quantity * bi.UnitPrice);

            var couponCode = HttpContext.Session.GetString(CouponSessionKey);
            Coupon? appliedCoupon = null;
            decimal discountAmount = 0;

            if (!string.IsNullOrEmpty(couponCode))
            {
                appliedCoupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);
                if (appliedCoupon != null)
                {
                    if (appliedCoupon.ExpiryDate.HasValue && appliedCoupon.ExpiryDate.Value < DateTime.UtcNow)
                    {
                        HttpContext.Session.Remove(CouponSessionKey);
                        appliedCoupon = null;
                        TempData["CouponError"] = "Tətbiq edilmiş kuponun istifadə müddəti bitmişdir.";
                    }
                    else if (appliedCoupon.MinOrderAmount.HasValue && subTotal < appliedCoupon.MinOrderAmount.Value)
                    {
                        var minAmount = appliedCoupon.MinOrderAmount.Value;
                        var code = appliedCoupon.Code;
                        HttpContext.Session.Remove(CouponSessionKey);
                        appliedCoupon = null;
                        TempData["CouponError"] = $"'{code}' kuponu üçün minimum sifariş məbləği {minAmount:N2} ₼ olmalıdır.";
                    }
                    else
                    {
                        if (appliedCoupon.DiscountType == DiscountType.Percentage)
                        {
                            discountAmount = Math.Round(subTotal * (appliedCoupon.DiscountValue / 100m), 2);
                        }
                        else
                        {
                            discountAmount = Math.Min(subTotal, appliedCoupon.DiscountValue);
                        }
                    }
                }
                else
                {
                    HttpContext.Session.Remove(CouponSessionKey);
                }
            }

            ViewBag.Subtotal = subTotal;
            ViewBag.AppliedCoupon = appliedCoupon;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.Total = Math.Max(0, subTotal - discountAmount);

            return View(basket);
        }

        // POST: /Basket/ApplyCoupon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                TempData["CouponError"] = "Zəhmət olmasa kupon kodunu daxil edin.";
                return RedirectToAction(nameof(Index));
            }

            couponCode = couponCode.Trim().ToUpper();

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);
            if (coupon == null || !coupon.IsActive)
            {
                TempData["CouponError"] = "Daxil edilən kupon kodu mövcud deyil və ya deaktivdir.";
                return RedirectToAction(nameof(Index));
            }

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
            {
                TempData["CouponError"] = "Bu kupon kodunun istifadə müddəti bitmişdir.";
                return RedirectToAction(nameof(Index));
            }

            var basket = await GetOrCreateBasketAsync();
            await SyncBasketItemPricesAsync(basket);
            var subTotal = basket.BasketItems.Sum(bi => bi.Quantity * bi.UnitPrice);

            if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value)
            {
                TempData["CouponError"] = $"'{coupon.Code}' kuponunu istifadə etmək üçün minimum səbət məbləği {coupon.MinOrderAmount.Value:N2} ₼ olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetString(CouponSessionKey, coupon.Code);
            TempData["CouponSuccess"] = $"'{coupon.Code}' kupon kodu uğurla tətbiq edildi!";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Basket/RemoveCoupon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove(CouponSessionKey);
            TempData["CouponSuccess"] = "Kupon kodu səbətdən silindi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Basket/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int? productVariantId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            int availableStock = product.StockQuantity;
            decimal variantModifier = 0;
            if (productVariantId.HasValue)
            {
                var variant = await _context.ProductVariants.FindAsync(productVariantId.Value);
                if (variant == null || variant.ProductId != productId) return NotFound();
                variantModifier = variant.PriceModifier;
                availableStock = variant.StockQuantity;
            }

            // Stok yoxlaması
            if (availableStock <= 0)
            {
                TempData["Error"] = "Seçilmiş məhsul və ya variantın stoku tükənib.";
                return RedirectToAction("Details", "Shop", new { id = productId });
            }

            var basket = await GetOrCreateBasketAsync();

            var existingItem = basket.BasketItems.FirstOrDefault(bi =>
                bi.ProductId == productId && bi.ProductVariantId == productVariantId);

            int currentInCart = existingItem?.Quantity ?? 0;
            if (currentInCart + quantity > availableStock)
            {
                if (currentInCart >= availableStock)
                {
                    TempData["Warning"] = $"Bu məhsuldan səbətinizdə artıq maksimum mövcud stok ({availableStock} ədəd) qədər var.";
                    return RedirectToAction("Index");
                }
                quantity = availableStock - currentInCart;
            }

            var basePrice = (product.DiscountPrice.HasValue && product.DiscountPrice.Value < product.Price)
                ? product.DiscountPrice.Value
                : product.Price;
            var activeUnitPrice = basePrice + variantModifier;

            if (existingItem != null)
            {
                existingItem.Quantity = Math.Min(availableStock, existingItem.Quantity + quantity);
                existingItem.UnitPrice = activeUnitPrice;
            }
            else
            {
                basket.BasketItems.Add(new BasketItem
                {
                    BasketId = basket.Id,
                    ProductId = productId,
                    ProductVariantId = productVariantId,
                    Quantity = Math.Min(availableStock, quantity),
                    UnitPrice = activeUnitPrice
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Basket/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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