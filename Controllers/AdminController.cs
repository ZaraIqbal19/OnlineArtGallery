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

        public IActionResult EditCustomers(string id)
        {
            var user = bridge.Users.Find(id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult EditLogic(string id, Art_GalleryUser model)
        {
            var user = bridge.Users.Find(id);

            if (user == null)
                return NotFound();

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.gender = model.gender;
            user.age = model.age;
            user.address = model.address;


            // Agar Name property hai to
            // user.Name = model.Name;

            bridge.Users.Update(user);
            bridge.SaveChanges();

            TempData["Message"] = "User Updated Successfully";

            return RedirectToAction("AllCustomers", "Admin");
        }
        public IActionResult DeleteCustomers(int Id)
        {

            return View(bridge.Users.Find(Id));
        }
        
        public IActionResult Deletelogic(string Id)
        {
            var Cus = bridge.Users.Find(Id);
            bridge.Users.Remove(Cus);
            bridge.SaveChanges();
            TempData["Message"] = "Customer deleted Successfully";
            return RedirectToAction("AllCustomers", "Admin");
        }


    }
}   