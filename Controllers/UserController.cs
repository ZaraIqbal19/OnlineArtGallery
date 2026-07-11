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

            var contact = new Contact() { 
            
            Message= Message,
            UserId=userId
            
            };
            bridge.contacts.Add(contact);
            bridge.SaveChanges();
            TempData["Message"] = "message sucessfully sent to admin";

            return RedirectToAction("Addcontact");
        }
    }
}