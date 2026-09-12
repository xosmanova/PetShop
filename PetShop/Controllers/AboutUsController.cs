using Microsoft.AspNetCore.Mvc;

namespace PetShop.Controllers
{
    public class AboutUsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
