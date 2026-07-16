using Art_Gallery.Areas.Identity.Data;
using Art_Gallery.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
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

        [HttpPost]
        public IActionResult AddCategorylogic(Category cat, IFormFile CategoryImageFile)
        {
            if (CategoryImageFile != null && CategoryImageFile.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "categories");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(CategoryImageFile.FileName);
                string fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    CategoryImageFile.CopyTo(stream);
                }

                cat.Categoryimage = "/uploads/categories/" + fileName;
            }

            bridge.categories.Add(cat);
            bridge.SaveChanges();

            ViewBag.Message = "Category added successfully";
            ViewBag.CategoryName = cat.Name;

            return View("AddCategory", new Category());
        }


        public IActionResult Editcategory(int id)
        {
            var category = bridge.categories.Find(id);
            return View(category);
        }

        public   IActionResult EditcategoryLogic(int id, string Name, IFormFile CategoryImageFile)
        {
            var category = bridge.categories.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            category.Name = Name;

            // Only replace image if a new file was uploaded
            if (CategoryImageFile != null && CategoryImageFile.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(CategoryImageFile.FileName);
                string filePath = Path.Combine("wwwroot/images/categories", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                     CategoryImageFile.CopyTo(stream);
                }

                category.Categoryimage = "/images/categories/" + fileName;
            }

            bridge.SaveChanges();

            return RedirectToAction("Allcategories");
        }

        [Authorize]

        public IActionResult Addsubcategories()
        {
            return View(bridge.categories.ToList());
        }
        [Authorize]

        [Authorize]
        public IActionResult Addsubcategorylogic(SubCategory subcat, IFormFile SubCategoryImageFile)
        {
            if (SubCategoryImageFile != null && SubCategoryImageFile.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "subcategories");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(SubCategoryImageFile.FileName);
                string fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    SubCategoryImageFile.CopyTo(stream);
                }

                subcat.SubCategoryimage = "/uploads/subcategories/" + fileName;
            }

            bridge.subCategories.Add(subcat);
            bridge.SaveChanges();

            TempData["Message"] = "Sub categories added successfully";
            return RedirectToAction("addsubcategories");
        }

        public IActionResult Editsubcategory(int id)
        {
         var subcategory = bridge.subCategories
                                   .Include(c => c.category) 
                                   .FirstOrDefault(c => c.Id == id);
            return View(subcategory);
        
        }
        
        public IActionResult Editsubcategorylogic(int id, int CategoryId, string Name, IFormFile SubCategoryImageFile)
        {
            var subCategory = bridge.subCategories.FirstOrDefault(c => c.Id == id);

            if (subCategory == null)
            {
                return NotFound();
            }

            subCategory.Name = Name;
            subCategory.CategoryId = CategoryId;  

            if (SubCategoryImageFile != null && SubCategoryImageFile.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(SubCategoryImageFile.FileName);
                string filePath = Path.Combine("wwwroot/images/subcategories", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                     SubCategoryImageFile.CopyTo(stream);
                }

                subCategory.SubCategoryimage = "/images/subcategories/" + fileName;
            }

            bridge.SaveChanges();

            return RedirectToAction("Allsubcategories");
        }











        public IActionResult Allcontacts()
        {
            var contacts = bridge.contacts.Include(c => c.User).ToList();
            return View(contacts);
        }








        public IActionResult Allproducts()
        {
            var products = bridge.products
                            .Include(p => p.SubCategory)
                               .ThenInclude(s => s.category)
                            .Include(p => p.User)
                            .ToList();
            return View(products);
        }
        public IActionResult Viewproductdetails(int id)
        {
            var productdetails = bridge.products
                                       .Include(p => p.SubCategory)
                                       .Include(p => p.User)
                                       .FirstOrDefault(p => p.Id == id);
            if (productdetails == null)
            {
                return NotFound();
            }

            return View(productdetails);
        }

        public IActionResult Deletelogic(int Id)
        {
            var Pro = bridge.products.Find(Id);
            bridge.products.Remove(Pro);
            bridge.SaveChanges();
            TempData["Message"] = "Product deleted Successfully";
            return RedirectToAction("Allproducts");
        }


        public IActionResult AllCustomers()
        {
            return View(bridge.Users.ToList());
        }

     

        public IActionResult Deletecustomerlogic(string id)
        {
            var user = bridge.Users.Find(id);
            if (user == null) return NotFound();

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

        public IActionResult Productrequest()
        {


            var Pendingproducts = bridge.products
                                            .Where(p => p.Status == "Pending")
                                            .Include(p => p.SubCategory)
                                            .Include(p => p.User)
                                            .ToList();
            return View(Pendingproducts);

                
        }
        public IActionResult Acceptrequestlogic(int id) { 
        
        var Product=bridge.products.Find(id);



            Product.Status = "Available";
            bridge.SaveChanges();
            TempData["Message"] = $"\"" + Product.Name + "\" has been approved and is now live.";

            return RedirectToAction("Productrequest");
      }

        public IActionResult Rejectrequestlogic(int id)
        {

            var Product = bridge.products.Find(id);

            Product.Status = "Rejected";
            bridge.SaveChanges();
            TempData["Warningmessage"] = $"\"" + Product.Name + "\" has been rejected.";

            return RedirectToAction("Productrequest");
        }


        public IActionResult Deleterequestlogic(int id)
        {

            var Product = bridge.products.Find(id);
            bridge.products.Remove(Product);
            bridge.SaveChanges();
            TempData["Message"] = $"\""+Product.Name+"\" has been deleted.";

            return RedirectToAction("Productrequest");
        }

        public IActionResult Allpaymentdetails()
 
        {
            var paymentdetails = bridge.paymentDetails
                               .Include(p => p.User)
                               .ToList();

            return View(paymentdetails);

        }

        public IActionResult Deletepaymentdetailslogic(int id)
        {

            var paymentdetails = bridge.paymentDetails.Find(id);
            bridge.paymentDetails.Remove(paymentdetails);
            bridge.SaveChanges();

            return RedirectToAction("Allpaymentdetails");
        }

    }
}   