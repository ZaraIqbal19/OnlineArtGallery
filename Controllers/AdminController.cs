using Microsoft.AspNetCore.Mvc;

namespace Art_Gallery.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Category()
        {
            return View();
        }

    }
}
