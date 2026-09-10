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

            Basket basket;

            if (string.IsNullOrEmpty(sessionId))
            {
                basket = new Basket { BasketItems = new List<BasketItem>() };
            }
            else
            {
                basket = await _context.Baskets
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId)
                    ?? new Basket { BasketItems = new List<BasketItem>() };
            }

            return View(basket);
        }
    }
}