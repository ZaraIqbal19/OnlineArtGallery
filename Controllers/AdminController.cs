using Art_Gallery.Areas.Identity.Data;
using Art_Gallery.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

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


        // all user work started from here 

        // function to display all user on AllCustomers page
        public IActionResult AllCustomers()
        {
            return View(bridge.Users.ToList());
        }
        //function to delete a Customer completely from the database all its products wishlist items payment details
        public IActionResult Deletecustomerlogic(string id)
        {
            var user = bridge.Users.Find(id);
            if (user == null) return NotFound();

            var userProductIds = bridge.products
                .Where(p => p.UserId == id)
                .Select(p => p.Id)
                .ToList();

            if (userProductIds.Any())
            {
                var wishlistForTheirProducts = bridge.wishlist
                    .Where(w => userProductIds.Contains(w.ProductId));
                bridge.wishlist.RemoveRange(wishlistForTheirProducts);
            }

            var userOwnWishlist = bridge.wishlist.Where(w => w.UserId == id);
            bridge.wishlist.RemoveRange(userOwnWishlist);

            var userProducts = bridge.products.Where(p => p.UserId == id);
            bridge.products.RemoveRange(userProducts);

            var userPaymentDetails = bridge.paymentDetails.Where(pd => pd.UserId == id);
            bridge.paymentDetails.RemoveRange(userPaymentDetails);

            bridge.Users.Remove(user);

            bridge.SaveChanges();

            TempData["Message"] = "Customer and their related records were deleted successfully.";
            return RedirectToAction("AllCustomers");
        }

        // all users work ended 





        //category work starts from here 



        //function to open add category page

        [Authorize]

        public IActionResult AddCategory()
        {
            return View();
        }

        //function to add category in database
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


        //function to display all categories on All categories page 

        public IActionResult Allcategories()
        {
            return View(bridge.categories.ToList());

        }




        //function to open  edit category page with values in input fields 
        public IActionResult Editcategory(int id)
        {
            var category = bridge.categories.Find(id);
            return View(category);
        }

        // function to save the edited values in the database
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
  

        //function to delete a category completely from the database 

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

        //category work ended


        //sub category work started from here 

        [Authorize]

        // function to open addsubcategories page with categories
        public IActionResult Addsubcategories()
        {
            return View(bridge.categories.ToList());
        }
        [Authorize]


        //function to save sub categories in database

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



        // funtion to fetch all subcategories with categories on Allsubcategories page

        public IActionResult Allsubcategories()
        {
            var data = bridge.subCategories
                      .Include(s => s.category)
                      .ToList();

            return View(data);
        }


        //function to open Editsubcategory page with values in the input fields
        public IActionResult Editsubcategory(int id)
        {
         var subcategory = bridge.subCategories
                           .Include(c => c.category) 
                           .FirstOrDefault(c => c.Id == id);
                            return View(subcategory);
        
        }
       // function to save the subcategory updated values int the database  
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


        // function to delete a sub category completely from the database

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

        // sub category work ended


        //product work started from here 


        //function to open addproduct page with categories and subcategories  

        public IActionResult Addproduct()
        {
            ViewBag.Categories = bridge.categories.ToList();
            ViewBag.SubCategories = bridge.subCategories.ToList();
            return View();
        }

        // function to add a product in the products table

        public IActionResult Addproductlogic(
    string Name, string Description,
    IFormFile Image1, IFormFile Image2, IFormFile Image3,
    float price, int quantity, string AvailableForBid,
    DateOnly BidStartDate, DateOnly BidEndDate, float BidPrice,
    int SubCategoryId, string Status)
        {
            var validImages = new List<IFormFile> { Image1, Image2, Image3 }
                .Where(f => f != null && f.Length > 0)
                .ToList();

            if (validImages.Count != 3)
            {
                TempData["Message"] = "Please upload all 3 images.";
                return RedirectToAction("Create");
            }

            if (AvailableForBid == "Yes")
            {
                if (BidStartDate == default || BidEndDate == default || BidPrice <= 0)
                {
                    TempData["Message"] = "Please fill in bid start date, end date, and bid price for an auction.";
                    return RedirectToAction("Create");
                }
                price = 0;
            }
            else
            {
                if (price <= 0)
                {
                    TempData["Message"] = "Please enter a valid price.";
                    return RedirectToAction("Create");
                }
                BidStartDate = default;
                BidEndDate = default;
                BidPrice = 0;
            }

            if (string.IsNullOrWhiteSpace(Status))
            {
                TempData["Message"] = "Please select a status.";
                return RedirectToAction("Create");
            }


            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string SaveImage(IFormFile file)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                return "/images/products/" + fileName;
            }

            List<string> savedImagePaths = validImages.Select(SaveImage).ToList();

            var product = new Models.Product()
            {
                Name = Name,
                Description = Description,
                Image1 = savedImagePaths.ElementAtOrDefault(0),
                Image2 = savedImagePaths.ElementAtOrDefault(1),
                Image3 = savedImagePaths.ElementAtOrDefault(2),
                price = price,
                quantity = quantity,
                AvailableForBid = AvailableForBid,
                BidStartDate = BidStartDate,
                BidEndDate = BidEndDate,
                BidPrice = BidPrice,
                SubCategoryId = SubCategoryId,
                UserId = userId,
                Status = Status
            };

            bridge.products.Add(product);
            bridge.SaveChanges();

            TempData["Message"] = "Product added successfully";
            return RedirectToAction("Allproducts");
        }


        //function to display all product on allproducts page

        public IActionResult Allproducts()
        {
            var products = bridge.products
                            .Include(p => p.SubCategory)
                               .ThenInclude(s => s.category)
                            .Include(p => p.User)
                            .ToList();
            return View(products);
        }

        //function to view a specific product detailed information
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


        //function to open Editproduct page with values in its input fields

        [Authorize]
        public IActionResult Editproduct(int Id)
        {
            var product = bridge.products.Find(Id);
            if (product == null)
                return NotFound();

            ViewBag.Categories = bridge.categories.ToList();
            ViewBag.SubCategories = bridge.subCategories.ToList();

            var currentSubCategory = bridge.subCategories.Find(product.SubCategoryId);
            ViewBag.CurrentCategoryId = currentSubCategory?.CategoryId;

            return View(product);
        }

        // function to save updated values in the products table

        public IActionResult Editproductlogic(
    int Id, string Name, string Description,
    float price, int quantity, string AvailableForBid,
    DateOnly BidStartDate, DateOnly BidEndDate, float BidPrice,
    int SubCategoryId,
    IFormFile Image1, IFormFile Image2, IFormFile Image3)
        {
            var prod = bridge.products.Find(Id);
            if (prod == null)
                return NotFound();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (prod.UserId != userId)
                return Forbid();

            if (AvailableForBid == "Yes")
            {
                if (BidStartDate == default || BidEndDate == default || BidPrice <= 0)
                {
                    TempData["Message"] = "Please fill in bid start date, end date, and bid price for an auction.";
                    return RedirectToAction("Edit", new { Id });
                }
                price = 0;
            }
            else
            {
                if (price <= 0)
                {
                    TempData["Message"] = "Please enter a valid price.";
                    return RedirectToAction("Edit", new { Id });
                }
                BidStartDate = default;
                BidEndDate = default;
                BidPrice = 0;
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string SaveImage(IFormFile file)
            {
                if (file == null || file.Length == 0)
                    return null;
                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                return "/images/products/" + fileName;
            }

            prod.Name = Name;
            prod.Description = Description;
            prod.price = price;
            prod.quantity = quantity;
            prod.SubCategoryId = SubCategoryId;
            prod.AvailableForBid = AvailableForBid;
            prod.BidStartDate = BidStartDate;
            prod.BidEndDate = BidEndDate;
            prod.BidPrice = BidPrice;

            var newImage1 = SaveImage(Image1);
            var newImage2 = SaveImage(Image2);
            var newImage3 = SaveImage(Image3);
            if (newImage1 != null) prod.Image1 = newImage1;
            if (newImage2 != null) prod.Image2 = newImage2;
            if (newImage3 != null) prod.Image3 = newImage3;

            bridge.products.Update(prod);
            bridge.SaveChanges();

            TempData["Message"] = "Product Updated Successfully";
            return RedirectToAction("Allproducts");
        }

        // function to delete a product completely from the database

        public IActionResult Deletelogic(int Id)
        {
            var Pro = bridge.products.Find(Id);
            bridge.products.Remove(Pro);
            bridge.SaveChanges();
            TempData["Message"] = "Product deleted Successfully";
            return RedirectToAction("Allproducts");
        }


        // function to fetch all the products from product table with pending status

        public IActionResult Productrequest()
        {


            var Pendingproducts = bridge.products
                                            .Where(p => p.Status == "Pending")
                                            .Include(p => p.SubCategory)
                                            .Include(p => p.User)
                                            .ToList();
            return View(Pendingproducts);


        }

        // function to approve a product. approving a product  will change its status from pending to available making it visible  on the all products page 

        public IActionResult Acceptrequestlogic(int id)
        {

            var Product = bridge.products.Find(id);



            Product.Status = "Available";
            bridge.SaveChanges();
            TempData["Message"] = $"\"" + Product.Name + "\" has been approved and is now live.";

            return RedirectToAction("Productrequest");
        }

        // function to reject a project,rejecting a product will change its status from pending to rejecting making it never visible on all products page 

        public IActionResult Rejectrequestlogic(int id)
        {

            var Product = bridge.products.Find(id);

            Product.Status = "Rejected";
            bridge.SaveChanges();
            TempData["Warningmessage"] = $"\"" + Product.Name + "\" has been rejected.";

            return RedirectToAction("Productrequest");
        }

        // function to delete a product request

        public IActionResult Deleterequestlogic(int id)
        {

            var Product = bridge.products.Find(id);
            bridge.products.Remove(Product);
            bridge.SaveChanges();
            TempData["Message"] = $"\"" + Product.Name + "\" has been deleted.";

            return RedirectToAction("Productrequest");
        }

        // product work ended 


        //auction bids work started from here


        // function to display all auction bids on Auctiondetails page

        public IActionResult Auctiondetails()
        {

            var auctiondetails = bridge.auctionDetails.Include(a => a.User)
    .Include(a => a.Product)
        .ThenInclude(p => p.SubCategory)
            .ThenInclude(sc => sc.category)
    .ToList();
            return View(auctiondetails);
        }


        //function to display a detailed a information of a bid

        public IActionResult ViewAuctiondetails(int id)
        {
            var auctionid = bridge.auctionDetails.Find(id);

            var details = bridge.auctionDetails.Include(d => d.User)
             .Include(d => d.Product)
              .ThenInclude(d => d.SubCategory)
                     .ThenInclude(dc => dc.category)
             .FirstOrDefault(d => d.Id == id);

            ViewBag.PreviousBids = bridge.auctionDetails
           .Include(b => b.User)
           .Where(b => b.ProductId == details.ProductId)
           .OrderByDescending(b => b.bidamount)
           .ToList();

            return View(details);


        }

        //function to permanentely a bid from the auctiondetails table

        public IActionResult Deleteauctiondetaillogic(int id)


        {
            var auctionid = bridge.auctionDetails.Find(id);


            bridge.auctionDetails.Remove(auctionid);
            bridge.SaveChanges();
            TempData["Message"] = "Auction bid sucessfully deleted";

            return RedirectToAction("Auctiondetails");

        }

        //auction bids work ended


        //user card details work started from here


        //function to display alll payments details on Allpaymentdetails page

        public IActionResult Allpaymentdetails()

        {
            var paymentdetails = bridge.paymentDetails
                               .Include(p => p.User)
                               .ToList();

            return View(paymentdetails);

        }

        //function to delete a user payment details completely from the paymentdetails table

        public IActionResult Deletepaymentdetailslogic(int id)
        {

            var paymentdetails = bridge.paymentDetails.Find(id);
            bridge.paymentDetails.Remove(paymentdetails);
            bridge.SaveChanges();

            return RedirectToAction("Allpaymentdetails");
        }

        //user card details work ended


        //admin products orders work started from here


        // function to display all products orders on Adminproductsorders page

        public IActionResult Adminproductsorders()
        {
            var orders = bridge.orders.Include(o => o.User)
    .Include(o => o.Product)
    .Include(o => o.Payment).ToList();
            return View(orders);
        }

        // function to display a detailed information related to order on Adminorderdetails page

        public IActionResult Adminorderdetails(int id)
        {


            var orderdetails = bridge.orders.Include(o => o.User)
    .Include(o => o.Payment)
        .Include(o => o.Product)
         .ThenInclude(p => p.SubCategory)
                .ThenInclude(sc => sc.category)
        .FirstOrDefault(o => o.Id == id);
            return View(orderdetails);

        }

        // function to permanently delete an order and its payments details if exists 

        public IActionResult Adminorderdeletelogic(int id)
        {
            var order = bridge.orders
                .Include(o => o.Payment)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Payment != null)
            {
                bridge.payments.Remove(order.Payment);
            }

            bridge.orders.Remove(order);

            bridge.SaveChanges();

            TempData["Message"] = "Order has been successfully deleted.";

            return RedirectToAction("Adminproductsorders");
        }


        //admin products orders work ended



        //orders payments work started from here 


        //function to display all orders paymnet details on Allorderspayments page 

        public IActionResult Allorderspayments()


        {
            var payments = bridge.payments
               .Include(p => p.Order)
                   .ThenInclude(o => o.User)
               .Include(p => p.Order)
                   .ThenInclude(o => o.Product)
               .ToList();
            return View(payments);
        }

        //function to permanently a order payment details

        public IActionResult Orderpaymentdeletelogic(int id)
        {

            var paymentid = bridge.payments.Find(id);

            bridge.payments.Remove(paymentid);
            bridge.SaveChanges();
            TempData["Message"] = "Order payment data has been successfully deleted.";

            return RedirectToAction("Allorderspayments");


        }

        //orders payments work ended


        // user contact messages work started from here 

        //function to display all users contact messages in Allcontacts page
        public IActionResult Allcontacts()
        {
            var contacts = bridge.contacts.Include(c => c.User).ToList();
            return View(contacts);
        }


        //function to delete a user contact message completely from the database

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

        // user contact work ended


       



       

      


    }
}   