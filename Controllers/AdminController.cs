using Art_Gallery.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Art_Gallery.Controllers
{
    public class AdminController : Controller
    {
       Art_GalleryContext bridge;

        public AdminController(Art_GalleryContext _bridge) { 
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
        [Authorize]

        public IActionResult Allproducts()
        {
            return View(bridge.products.ToList());
        }


    }
}