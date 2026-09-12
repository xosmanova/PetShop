using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.ViewComponents
{
    public class BasketSummaryViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        private const string SessionKey = "BasketSessionId";

        public BasketSummaryViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var sessionId = HttpContext.Session.GetString(SessionKey);
            var userId = HttpContext.User.Identity?.IsAuthenticated == true
                ? HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            Basket? basket = null;

            if (!string.IsNullOrEmpty(userId))
            {
                basket = await _context.Baskets
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.Images)
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.ProductVariant)
                    .FirstOrDefaultAsync(b => b.UserId == userId);
            }

            if (basket == null && !string.IsNullOrEmpty(sessionId))
            {
                basket = await _context.Baskets
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.Images)
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.ProductVariant)
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.UserId == null);
            }

            basket ??= new Basket { BasketItems = new List<BasketItem>() };

            return View(basket);
        }
    }
}