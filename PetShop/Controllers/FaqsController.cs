using Microsoft.AspNetCore.Mvc;

namespace PetShop.Controllers
{
    public class FaqsController : Controller
    {
        // GET: /Faqs
        public IActionResult Index()
        {
            return View();
        }
    }
}
