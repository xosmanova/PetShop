using Microsoft.AspNetCore.Mvc;

namespace PetShop.Controllers
{
    public class DeliveryController : Controller
    {
        // GET: /Delivery
        public IActionResult Index()
        {
            return View();
        }
    }
}
