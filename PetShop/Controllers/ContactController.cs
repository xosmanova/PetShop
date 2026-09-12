using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace PetShop.Controllers
{
    public class ContactController : Controller
    {
        private readonly IConfiguration _configuration;

        public ContactController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessage(string name, string email, string subject, string message)
        {
            TempData["SuccessMessage"] = "Mesajınız uğurla göndərildi! Ən qısa zamanda sizinlə əlaqə saxlayacağıq.";
            return RedirectToAction(nameof(Index));
        }
    }
}
