using Microsoft.AspNetCore.Mvc;

namespace Art_Gallery.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Gallery()
        {
            return View();
        }
        public IActionResult Events()
        {
            return View();
        }
        public IActionResult Tickets()
        {
            return View();
        }
        public IActionResult Contacts()
        {
            return View();
        }


    }
}
