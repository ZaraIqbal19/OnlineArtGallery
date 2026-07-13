using Art_Gallery.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace Art_Gallery.Controllers
{
    public class UserController : Controller
    {
        Art_GalleryContext bridge;

        public UserController(Art_GalleryContext _bridge)
        {
            bridge = _bridge;
        }

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
        [Authorize]

        public IActionResult Addproduct()
        {
            ViewBag.Categories = bridge.categories.ToList();
            ViewBag.SubCategories = bridge.subCategories.ToList();
            return View();
        }

        [Authorize]
        public IActionResult Addproductlogic(
            string Name, string Description,
            IFormFile Image1, IFormFile Image2, IFormFile Image3,
            float price, int quantity, string AvailableForBid, DateOnly BidStartDate,
            DateOnly BidEndDate, float BidPrice, int SubCategoryId)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Helper local function — har image ke liye reuse hoga
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

            string image1Path = SaveImage(Image1);
            string image2Path = SaveImage(Image2);
            string image3Path = SaveImage(Image3);

            var product = new Product()
            {
                Name = Name,
                Description = Description,
                Image1 = image1Path,
                Image2 = image2Path,
                Image3 = image3Path,
                price = price,
                quantity = quantity,
                AvailableForBid = AvailableForBid,
                BidStartDate = BidStartDate,
                BidEndDate = BidEndDate,
                BidPrice = BidPrice,
                SubCategoryId = SubCategoryId,
                UserId = userId,
                Status = "Pending"
            };

            bridge.products.Add(product);
            bridge.SaveChanges();
            TempData["Message"] = "product added sucessfully ";

            return RedirectToAction("Addproduct");
        }
        public IActionResult Allproducts()
        {
            return View(bridge.products.ToList());
        }

        public IActionResult Addpaymentdetails()
        {
            return View();
        }

        public IActionResult Addpaymentdetailslogic(int CardNumber, string CardTitle, int CVVCode, DateOnly DateofExpiry)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var paymentdetails = new Payment_Details()
            {
                CardNumber = CardNumber,
                CardTitle = CardTitle,
                CVVCode = CVVCode,
                DateofExpiry = DateofExpiry,
                UserId = userId

            };
            bridge.paymentDetails.Add(paymentdetails);
            bridge.SaveChanges();
            TempData["Message"] = "product added sucessfully ";

            return RedirectToAction("Addpaymentdetails");
        }

        public IActionResult Addcontact()
        {
            return View();
        }

        public IActionResult Addcontactlogic(String Message)
        {

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var contact = new Contact()
            {

                Message = Message,
                UserId = userId

            };
            bridge.contacts.Add(contact);
            bridge.SaveChanges();
            TempData["Message"] = "message sucessfully sent to admin";

            return RedirectToAction("Addcontact");
        }
        [Authorize]
        public IActionResult Myproducts()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var products = bridge.products.Include(p => p.SubCategory).Include(p => p.User)
            .Where(p => p.UserId == userId).ToList();
            return View(products);
        }



        public IActionResult Allcategories()
        {

            ViewBag.Categories = bridge.categories.ToList();
            ViewBag.SubCategories = bridge.subCategories.ToList();

            return View(ViewBag.Categories);
        }


        [Authorize]
        public IActionResult Edit(int Id)
        {
            var product = bridge.products.Find(Id);
            if (product == null)
                return NotFound();

            ViewBag.SubCategories = bridge.subCategories.ToList();
            return View(product);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Editlogic(int Id, Product pro, IFormFile Image1, IFormFile Image2, IFormFile Image3)
        {
            var prod = bridge.products.Find(Id);
            if (prod == null)
                return NotFound();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (prod.UserId != userId)
                return Forbid();

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

            prod.Name = pro.Name;
            prod.Description = pro.Description;
            prod.price = pro.price;
            prod.quantity = pro.quantity;
            prod.SubCategoryId = pro.SubCategoryId;
            prod.AvailableForBid = pro.AvailableForBid;
            prod.BidStartDate = pro.BidStartDate;
            prod.BidEndDate = pro.BidEndDate;
            prod.BidPrice = pro.BidPrice;

            var newImage1 = SaveImage(Image1);
            var newImage2 = SaveImage(Image2);
            var newImage3 = SaveImage(Image3);

            if (newImage1 != null) prod.Image1 = newImage1;
            if (newImage2 != null) prod.Image2 = newImage2;
            if (newImage3 != null) prod.Image3 = newImage3;

            // UserId and Status are intentionally left untouched — backend-controlled

            bridge.products.Update(prod);
            bridge.SaveChanges();

            TempData["Message"] = "Product Updated Successfully";
            return RedirectToAction("Myproducts");
        }
        public IActionResult Delete(int Id)
        {
            return View(bridge.products.Find(Id));
        }
        public IActionResult Deletelogic(int Id)
        {
            var Pro = bridge.products.Find(Id);
            bridge.products.Remove(Pro);
            bridge.SaveChanges();
            TempData["Message"] = "Product deleted Successfully";
            return RedirectToAction("Addproduct", "User");
        }
    }
}
      
    