using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;

namespace PetShop.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;
        private const string SessionKey = "BasketSessionId";

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sessionId = HttpContext.Session.GetString(SessionKey);

            if (string.IsNullOrEmpty(sessionId))
                return RedirectToAction("Index", "Basket");

            var basket = await _context.Baskets
                .Include(b => b.BasketItems)
                    .ThenInclude(bi => bi.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(b => b.SessionId == sessionId);

            if (basket == null || !basket.BasketItems.Any())
                return RedirectToAction("Index", "Basket");

            return View(basket);
        }

        [HttpPost]
        public IActionResult PlaceOrder(string firstname, string lastname, string address, string city, string phone, string email, string paymentMethod)
        {
            // ⚠️ Hələ Order entity-si yoxdur — bu, sonrakı mərhələdə (Login/Register-dən sonra) tamamlanacaq
            TempData["OrderMessage"] = "Sifarişiniz qeydə alındı! (Test rejimi)";
            return RedirectToAction("Index", "Home");
        }
    }
}
