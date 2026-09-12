using Microsoft.AspNetCore.Mvc;

namespace PetShop.Controllers
{
    public class PaymentController : Controller
    {
        // GET: /Payment
        public IActionResult Index()
        {
            return View();
        }
    }
}
