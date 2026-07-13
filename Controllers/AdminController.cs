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

        public IActionResult Allcontacts()
        {
            var contacts = bridge.contacts.Include(c => c.User).ToList();
            return View(contacts);
        }







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
<<<<<<< HEAD

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
=======
        public IActionResult Deletecustomerlogic(string id)
>>>>>>> 47cd3f71042134ea61ceebc9b26b0edbdb9459f0
        {
            var user = bridge.Users.Find(id);
            if (user == null) return NotFound();

<<<<<<< HEAD
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


=======
            bridge.Users.Remove(user);
            bridge.SaveChanges();

            TempData["Message"] = "Customer deleted successfully.";
            return RedirectToAction("AllCustomers");
        }
        public IActionResult Deletecontactlogic(int Id)
        {
            var Contactid = bridge.contacts.Find(Id);

            if (Contactid == null)
            {
                return NotFound();
            }

            bridge.contacts.Remove(Contactid);
            bridge.SaveChanges();
            TempData["Message"] = "Contact deleted Successfully";
            return RedirectToAction("Allcontacts");

        }

        public IActionResult Allcategories()
        {
            return View(bridge.categories.ToList());

        }

        public IActionResult Deletecategorylogic(int id)
        {
            var category = bridge.categories.Find(id);

            if (category == null)
            {
                TempData["Errormessage"] = "Category not found.";
                return RedirectToAction("Allcategories");
            }

            bridge.categories.Remove(category);
            bridge.SaveChanges();

            TempData["Message"] = "Category deleted successfully";
            return RedirectToAction("Allcategories");
        }


        public IActionResult Allsubcategories()
        {
            var data = bridge.subCategories
                      .Include(s => s.category)
                      .ToList();

            return View(data);
        }

        public IActionResult Deletesubcategorylogic(int id)
        {
            var subcategory = bridge.subCategories.Find(id);

            if (subcategory == null)
            {
                TempData["Errormessage"] = "SubCategory not found.";
                return RedirectToAction("Allcategories"); 
            }

            bridge.subCategories.Remove(subcategory);
            bridge.SaveChanges();

            TempData["Message"] = "SubCategory deleted successfully";
            return RedirectToAction("Allsubcategories");
        }

>>>>>>> 47cd3f71042134ea61ceebc9b26b0edbdb9459f0
    }
}   