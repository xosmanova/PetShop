using Microsoft.AspNetCore.Mvc;

namespace PetShop.Controllers
{
    public class ReturnsController : Controller
    {
        // GET: /Returns
        public IActionResult Index()
        {
            return View();
        }
    }
}
