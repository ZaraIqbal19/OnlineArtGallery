using Art_Gallery.Areas.Identity.Data;
using Art_Gallery.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Art_Gallery.Controllers
{
    public class AdminController : Controller
    {
        Art_GalleryContext bridge;

        public AdminController(Art_GalleryContext _bridge)
        {
            bridge = _bridge;
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]

        public IActionResult AddCategory()
        {
            return View();
        }

        public IActionResult AddCategorylogic(Category cat)
        {
            bridge.categories.Add(cat);
            bridge.SaveChanges();
            ViewBag.Message = "Category added successfully";
            return View("AddCategory");
        }
        [Authorize]

        public IActionResult Addsubcategories()
        {
            return View(bridge.categories.ToList());
        }
        [Authorize]

        public IActionResult Addsubcategorylogic(SubCategory subcat)
        {
            bridge.subCategories.Add(subcat);
            bridge.SaveChanges();
            TempData["Message"] = "Sub categories added successfully";
            return RedirectToAction("addsubcategories");
        }

        //public IActionResult AlLCategories() {

            
        //    return View(bridge.subCategories.ToList());
        //}\\

     





    [Authorize]

            public IActionResult Allproducts()
        {
            return View(bridge.products.ToList());
        }


        public IActionResult AllCustomers()
        {
            return View(bridge.Users.ToList());
        }

        public IActionResult EditCustomers()
        {
            return View();
        }
        public IActionResult DeleteCustomers()
        {

            return View();
        }
    }
}   