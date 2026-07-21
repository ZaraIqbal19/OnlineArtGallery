using Art_Gallery.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
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
        [HttpPost]
        public IActionResult Addproductlogic(
           string Name, string Description,
           List<IFormFile> Images,
           float price, int quantity, string AvailableForBid,
           DateOnly BidStartDate, DateOnly BidEndDate, float BidPrice,
           int SubCategoryId)
        {
            // 1. Validate exactly 3 images
            var validImages = Images?.Where(f => f != null && f.Length > 0).ToList() ?? new List<IFormFile>();
            if (validImages.Count != 3)
            {
                TempData["Message"] = "Please upload exactly 3 images.";
                return RedirectToAction("Addproduct");
            }

            // 2. Validate sale type fields
            if (AvailableForBid == "Yes")
            {
                // Auction: price not applicable, bid fields required
                if (BidStartDate == default || BidEndDate == default || BidPrice <= 0)
                {
                    TempData["Message"] = "Please fill in bid start date, end date, and bid price for an auction.";
                    return RedirectToAction("Addproduct");
                }
                price = 0; // ignore any price value submitted
            }
            else
            {
                // Fixed price: price required, bid fields not applicable
                if (price <= 0)
                {
                    TempData["Message"] = "Please enter a valid price.";
                    return RedirectToAction("Addproduct");
                }
                BidStartDate = default;
                BidEndDate = default;
                BidPrice = 0;
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
                Status = "Pending"
            };

            bridge.products.Add(product);
            bridge.SaveChanges();
            TempData["Message"] = "Product added successfully";
            return RedirectToAction("Addproduct");
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

            // Needed for the Category -> SubCategory cascading dropdown (same UI as Add)
            ViewBag.Categories = bridge.categories.ToList();
            ViewBag.SubCategories = bridge.subCategories.ToList();

            // So the view can pre-select the right Category for this product's current SubCategory
            var currentSubCategory = bridge.subCategories.Find(product.SubCategoryId);
            ViewBag.CurrentCategoryId = currentSubCategory?.CategoryId;

            return View(product);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Editlogic(
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

            // --- Same sale-type normalization as Addproductlogic ---
            // This was missing before, which is why stale price/bid values could survive an edit.
            if (AvailableForBid == "Yes")
            {
                if (BidStartDate == default || BidEndDate == default || BidPrice <= 0)
                {
                    TempData["Message"] = "Please fill in bid start date, end date, and bid price for an auction.";
                    return RedirectToAction("Edit", new { Id });
                }
                price = 0; // ignore any price value submitted
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


        public IActionResult Allproducts()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var products = bridge.products
                                          .Where(p => p.Status == "Available" && p.UserId != userid)
                                          .Include(p => p.SubCategory)
                                          .ToList();

            return View(products);
        }


        public IActionResult Mywishlist()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userwishlist = bridge.wishlist
                .Include(w => w.Product)
                .Where(w => w.UserId == userid)
                .ToList();

            return View(userwishlist);

        }


        public IActionResult Addtowishlistlogic(int id)
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var newwishlistitem = new Wishlist
            {
                UserId = userid,
                ProductId = id,
                Quantity = 1,
            };

            bridge.wishlist.Add(newwishlistitem);
            bridge.SaveChanges();
            return RedirectToAction("Mywishlist");


        }

        public IActionResult Removefromwishlistlogic(int id)
        {

            var wishlistitem = bridge.wishlist.Find(id);
            bridge.wishlist.Remove(wishlistitem);
            bridge.SaveChanges();

            return RedirectToAction("Mywishlist");

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


        public IActionResult Placeorder(int id)
        {
            var product = bridge.products
                .Include(p => p.SubCategory)
                .Include(p => p.User)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            ViewBag.RelatedProducts = bridge.products
                .Include(p => p.SubCategory)
                .Include(p => p.User)
                .Where(p => p.SubCategoryId == product.SubCategoryId && p.Id != product.Id)
                .Take(3)
                .ToList(); 


            return View(product);
        }
        
        public IActionResult Placeorderlogic(int ProductId, String ContactPhone,String ShippingAddress, int Quantity, int? WishlistId,String ModeofPayment) 

        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
          var prodid=  bridge.products.Find(ProductId);

            var price = prodid.price;
            decimal pricePaid = Convert.ToDecimal(prodid.price) * Quantity;

            if (ModeofPayment == "Card")
            {
                var paymentDetails = bridge.paymentDetails
                    .FirstOrDefault(p => p.UserId == userid);

                if (paymentDetails == null)
                {
                    TempData["Message"] = "Please add card details before ordering.";

                    return RedirectToAction("Placeorder", new { id = ProductId });
                }
            }
             var order =new Order()
            {   
                WishlistId = WishlistId,
                ProductId=ProductId,
                UserId=userid,
                OrderDate = DateTime.Now,
                ContactPhone=ContactPhone,
                ShippingAddress=ShippingAddress,
                Quantity=Quantity,
                PricePaid=pricePaid,

            };
            bridge.orders.Add(order);
            bridge.SaveChanges();

            var payment = new Payment()
            {
                ModeofPayment=ModeofPayment,
                OrderId=order.Id,
            };
            bridge.payments.Add(payment);
            bridge.SaveChanges();
            TempData["Orderpaymentmessage"] = "Your order has been placed successfully.";

            return RedirectToAction("Placeorder", new { id = ProductId });
        }


        public IActionResult Placebid(int id)
        {
            var product = bridge.products
                .Include(p => p.SubCategory)
                .Include(p => p.User)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.PreviousBids = bridge.auctionDetails
                .Include(b => b.User)
                .Where(b => b.ProductId == id)
                .OrderByDescending(b => b.bidamount)
                .ToList();

            ViewBag.RelatedBidProducts = bridge.products
                .Where(p => p.SubCategoryId == product.SubCategoryId
                         && p.Id != product.Id
                         && p.AvailableForBid == "Yes")
                .Take(8)
                .ToList();

            return View(product);
        }

    
        public IActionResult Placebidlogic(int id, float bidamount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = bridge.products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            if (product.AvailableForBid != "Yes")
            {
                TempData["ErrorMessage"] = "This item is not open for bidding.";
                return RedirectToAction("Placebid", new { id });
            }

            var highestBid = bridge.auctionDetails
                .Where(b => b.ProductId == id)
                .Select(b => (float?)b.bidamount)
                .Max() ?? product.BidPrice;

            if (bidamount < highestBid + 50)
            {
                TempData["ErrorMessage"] = "Your bid must be higher than the current bid.";
                return RedirectToAction("Placebid", new { id });
            }

            // Save the new bid
            var newBid = new AuctionDetails
            {
                UserId = userId,
                ProductId = id,
                bidamount = bidamount,
                bidstatus = "Pending"
            };

            product.BidPrice = bidamount;
           



            bridge.auctionDetails.Add(newBid);
            bridge.SaveChanges();

            TempData["SuccessMessage"] = "Your bid was placed successfully.";
            return RedirectToAction("Placebid", new { id });
        }


    

    }
}
      
    