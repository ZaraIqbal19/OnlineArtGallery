using Art_Gallery.Areas.Identity.Data;
using Art_Gallery.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using static NuGet.Packaging.PackagingConstants;
using static System.Net.Mime.MediaTypeNames;
namespace Art_Gallery.Controllers
{
    public class UserController : Controller
    {
        Art_GalleryContext bridge;
        // ✅
        private readonly IPasswordHasher<Art_GalleryUser> _passwordHasher;

        public UserController(Art_GalleryContext _bridge, IPasswordHasher<Art_GalleryUser> passwordHasher)
        {
            bridge = _bridge;
            _passwordHasher = passwordHasher;
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

        public IActionResult Placeorderlogic(int ProductId, String ContactPhone, String ShippingAddress, int Quantity, int? WishlistId, String ModeofPayment)

        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var prodid = bridge.products.Find(ProductId);

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
            var order = new Order()
            {
                WishlistId = WishlistId,
                ProductId = ProductId,
                UserId = userid,
                OrderDate = DateTime.Now,
                ContactPhone = ContactPhone,
                ShippingAddress = ShippingAddress,
                Quantity = Quantity,
                PricePaid = pricePaid,

            };
            bridge.orders.Add(order);
            bridge.SaveChanges();

            var payment = new Payment()
            {
                ModeofPayment = ModeofPayment,
                OrderId = order.Id,
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


        public IActionResult Myorders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = bridge.orders
                .Include(o => o.Product)
                .Include(o => o.Payment)
                .Include(o => o.User)
                .Where(o => o.UserId == userId)
                .ToList();

            return View(orders);
        }


        public IActionResult Myorderdetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var myorderdetails = bridge.orders
       .Include(o => o.User)
       .Include(o => o.Payment)
       .Include(o => o.Product)
           .ThenInclude(p => p.SubCategory)
               .ThenInclude(sc => sc.category)
       .FirstOrDefault(o => o.Id == id && o.UserId == userId);
            return View(myorderdetails);

        }


      

        public IActionResult Myproductsorders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sales = bridge.orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.Product)
                .Where(o => o.Product.UserId == userId)
                .ToList();

            return View(sales);
        }



        public IActionResult Myproductsorderdetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orderdetails = bridge.orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.Product)
                    .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.category)
                .FirstOrDefault(o => o.Id == id && o.Product.UserId == userId);


            return View(orderdetails);
        }

        public IActionResult markorderasprocessinglogic(int id)
        {
            var order = bridge.orders.Find(id);
            if (order == null) return NotFound();
            order.Status = "Processing";
            bridge.SaveChanges();

            TempData["Message"] = $"Order marked as  {order.Status} successfully.";


            return RedirectToAction("Myproductsorderdetails", new { id = id });
        }

        public IActionResult markorderasdispatchedlogic(int id)
        {
            var order = bridge.orders.Find(id);
            if (order == null) return NotFound();

            order.Status = "Dispatched";
            bridge.SaveChanges();

            TempData["Message"] = $"Order marked as  {order.Status} successfully.";


            return RedirectToAction("Myproductsorderdetails", new { id = id });
        }

        public IActionResult markorderasdeliveredlogic(int id)
        {
            var order = bridge.orders.Find(id);
            if (order == null) return NotFound();

            order.Status = "Delivered";
            bridge.SaveChanges();

            TempData["Message"] = $"Order marked as {order.Status} successfully.";


            return RedirectToAction("Myproductsorderdetails", new { id = id });
        }

        public IActionResult markorderasrejectedlogic(int id)
        {
            var order = bridge.orders.Find(id);


            order.Status = "Rejected";
            bridge.SaveChanges();

            TempData["Message"] = $"Order marked as  {order.Status} successfully.";



            return RedirectToAction("Myproductsorderdetails", new { id = id });
        }



        public IActionResult Myproductsorderdeletelogic(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = bridge.orders
                .Include(o => o.Payment)
                .Include(o => o.Product)
                .FirstOrDefault(o => o.Id == id && o.Product.UserId == userId);



            if (order.Payment != null)
            {
                bridge.payments.Remove(order.Payment);
            }

            bridge.orders.Remove(order);

            bridge.SaveChanges();

            TempData["Message"] = "Order has been successfully deleted.";

            return RedirectToAction("Myproductsorders");
        }


        public IActionResult Myauctionproductsbids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var auctiondetails = bridge.auctionDetails
                   .Include(a => a.User)
                   .Include(a => a.Product)
                       .ThenInclude(p => p.SubCategory)
                           .ThenInclude(sc => sc.category)
                   .Where(a => a.Product.UserId == userId)
                   .ToList();
            return View(auctiondetails);
        }


        //function to display a detailed a information of a bid

        public IActionResult Auctionproductsbiddetails(int id)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var biddetails = bridge.auctionDetails
          .Include(d => d.User)
          .Include(d => d.Product)
              .ThenInclude(p => p.SubCategory)
                  .ThenInclude(sc => sc.category)
          .FirstOrDefault(d => d.Id == id && d.Product.UserId == userId);



            ViewBag.PreviousBids = bridge.auctionDetails
                .Include(b => b.User)
                .Where(b => b.ProductId == biddetails.ProductId)
                .OrderByDescending(b => b.bidamount)
                .ToList();

            return View(biddetails);



        }

        //function to permanentely a bid from the auctiondetails table

        public IActionResult Auctionproductsbiddeletelogic(int id)


        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bid = bridge.auctionDetails
                .Include(a => a.Product)
                .FirstOrDefault(a => a.Id == id && a.Product.UserId == userId);


            bridge.auctionDetails.Remove(bid);
            bridge.SaveChanges();

            TempData["Message"] = "Auction bid successfully deleted.";

            return RedirectToAction("Myauctionproductsbids");

        }



        public IActionResult Myprofile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = bridge.Users.FirstOrDefault(x => x.Id == userId);

            var orders = bridge.orders
                .Include(o => o.Product)
                .Include(o => o.Payment)
                .Where(o => o.UserId == userId)
                .ToList();

            var wishlist = bridge.wishlist.Include(w => w.Product).Where(w => w.UserId == userId).ToList();
            var bids = bridge.auctionDetails.Include(b => b.Product).Where(b => b.UserId == userId).ToList();

            ViewBag.User = user;
            ViewBag.Orders = orders;
            ViewBag.Wishlist = wishlist;
            ViewBag.Bids = bids;

            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalWishlist = wishlist.Count;
            ViewBag.TotalBids = bids.Count;
            ViewBag.PendingOrders = orders.Count(x => x.Status == "Pending");
            ViewBag.ProcessingOrders = orders.Count(x => x.Status == "Processing");
            ViewBag.DispatchedOrders = orders.Count(x => x.Status == "Dispatched");
            ViewBag.DeliveredOrders = orders.Count(x => x.Status == "Delivered");
            ViewBag.CancelledOrders = orders.Count(x => x.Status == "Cancelled");
            ViewBag.TotalItemsPurchased = orders.Sum(x => x.Quantity);
            ViewBag.TotalMoneySpent = orders.Sum(x => x.PricePaid);
            ViewBag.AverageOrderValue = orders.Count > 0 ? orders.Average(x => x.PricePaid) : 0;
            ViewBag.HighestOrder = orders.Count > 0 ? orders.Max(x => x.PricePaid) : 0;
            ViewBag.LowestOrder = orders.Count > 0 ? orders.Min(x => x.PricePaid) : 0;
            ViewBag.PaidOrders = orders.Count(x => x.Payment != null);
            ViewBag.UnpaidOrders = orders.Count(x => x.Payment == null);
            ViewBag.CODOrders = orders.Count(x => x.Payment != null && x.Payment.ModeofPayment == "Cash On Delivery");
            ViewBag.CardOrders = orders.Count(x => x.Payment != null && x.Payment.ModeofPayment == "Card");
            ViewBag.UniqueProductsPurchased = orders.Select(x => x.ProductId).Distinct().Count();
            ViewBag.FirstOrderDate = orders.Any() ? (DateTime?)orders.Min(x => x.OrderDate) : null;
            ViewBag.LastOrderDate = orders.Any() ? (DateTime?)orders.Max(x => x.OrderDate) : null;

            // ---- Monthly spending trend (last 6 months) ----
            var monthlySpending = orders
                .Where(o => o.OrderDate >= DateTime.Now.AddMonths(-5).Date)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Total = g.Sum(x => x.PricePaid)
                })
                .ToList();

            ViewBag.MonthlySpendingLabels = monthlySpending.Select(x => x.Label).ToList();
            ViewBag.MonthlySpendingValues = monthlySpending.Select(x => x.Total).ToList();

            decimal lastMonthTotal = monthlySpending.Count > 0 ? monthlySpending[monthlySpending.Count - 1].Total : 0;
            decimal prevMonthTotal = monthlySpending.Count > 1 ? monthlySpending[monthlySpending.Count - 2].Total : 0;
            ViewBag.SpendingTrendPercent = prevMonthTotal > 0
                ? Math.Round((double)((lastMonthTotal - prevMonthTotal) / prevMonthTotal) * 100, 1)
                : (double?)null;

            // ---- Top 5 products by quantity purchased ----
            var topProducts = orders
                .Where(o => o.Product != null)
                .GroupBy(o => o.Product.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Qty = g.Sum(x => x.Quantity),
                    Spent = g.Sum(x => x.PricePaid)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToList();

            ViewBag.TopProductNames = topProducts.Select(x => x.Name).ToList();
            ViewBag.TopProductQty = topProducts.Select(x => x.Qty).ToList();
            ViewBag.TopProductSpent = topProducts.Select(x => x.Spent).ToList();
            ViewBag.TopProductMaxQty = topProducts.Count > 0 ? topProducts.Max(x => x.Qty) : 1;

            // ---- Wishlist & bidding insights ----
            ViewBag.WishlistCount = wishlist.Count;
            ViewBag.TotalBidsCount = bids.Count;
            ViewBag.HighestBidAmount = bids.Any() ? bids.Max(b => b.bidamount) : 0;
            ViewBag.ActiveBidsCount = bids.Count(b => b.bidstatus == "Pending");
            ViewBag.WonBidsCount = bids.Count(b => b.bidstatus == "Won");

            return View();
        }


        [HttpPost]
        public IActionResult UpdateProfile(string UserName, string Email, string PhoneNumber, string Address, int Age, string Gender)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = bridge.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null) return NotFound();

            user.UserName = UserName;
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;
            user.address = Address;
            user.age = Age;
            user.gender = Gender;

            bridge.SaveChanges();
            TempData["ProfileSuccess"] = "Profile updated successfully.";
            return RedirectToAction("Myprofile");
        }


        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // ClaimsPrincipal — correct use of capital "User" here
            var user = bridge.Users.FirstOrDefault(x => x.Id == userId);  // your entity — lowercase "user"
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                TempData["PasswordError"] = "All fields are required.";
                return RedirectToAction("Myprofile");
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, CurrentPassword);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                TempData["PasswordError"] = "Current password is incorrect.";
                return RedirectToAction("Myprofile");
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["PasswordError"] = "New password and confirmation do not match.";
                return RedirectToAction("Myprofile");
            }

            if (NewPassword.Length < 6)
            {
                TempData["PasswordError"] = "New password must be at least 6 characters long.";
                return RedirectToAction("Myprofile");
            }

            if (NewPassword == CurrentPassword)
            {
                TempData["PasswordError"] = "New password must be different from the current password.";
                return RedirectToAction("Myprofile");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, NewPassword);
            bridge.SaveChanges();

            TempData["PasswordSuccess"] = "Password updated successfully.";
            return RedirectToAction("Myprofile");
        }

        public IActionResult Usersellerprofile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Seller Information
            var seller = bridge.Users.FirstOrDefault(x => x.Id == userId);

            // Products uploaded by seller
            var products = bridge.products
                .Include(p => p.SubCategory)
                .Where(p => p.UserId == userId)
                .ToList();

            var productIds = products.Select(x => x.Id).ToList();

            // Orders of seller's products
            var orders = bridge.orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .Include(o => o.Payment)
                .Where(o => productIds.Contains(o.ProductId))
                .ToList();

            // Bids on seller's products
            var bids = bridge.auctionDetails
                .Include(b => b.User)
                .Include(b => b.Product)
                .Where(b => productIds.Contains(b.ProductId))
                .ToList();

            ViewBag.Seller = seller;
            ViewBag.Products = products;
            ViewBag.Orders = orders;
            ViewBag.Bids = bids;

            // Product Analytics
            ViewBag.TotalProducts = products.Count;
            ViewBag.AvailableProducts = products.Count(x => x.quantity > 0);
            ViewBag.OutOfStockProducts = products.Count(x => x.quantity == 0);

            // Sales Analytics
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalItemsSold = orders.Sum(x => x.Quantity);
            ViewBag.TotalRevenue = orders.Sum(x => x.PricePaid);

            ViewBag.AverageOrderValue = orders.Any()
                ? orders.Average(x => x.PricePaid)
                : 0;

            ViewBag.HighestSale = orders.Any()
                ? orders.Max(x => x.PricePaid)
                : 0;

            ViewBag.LowestSale = orders.Any()
                ? orders.Min(x => x.PricePaid)
                : 0;

            // Order Status
            ViewBag.Pending = orders.Count(x => x.Status == "Pending");
            ViewBag.Processing = orders.Count(x => x.Status == "Processing");
            ViewBag.Dispatched = orders.Count(x => x.Status == "Dispatched");
            ViewBag.Delivered = orders.Count(x => x.Status == "Delivered");
            ViewBag.Cancelled = orders.Count(x => x.Status == "Cancelled");

            // Payment Analytics
            ViewBag.CardPayments = orders.Count(x => x.Payment != null &&
                                                     x.Payment.ModeofPayment == "Card");

            ViewBag.CODPayments = orders.Count(x => x.Payment != null &&
                                                    x.Payment.ModeofPayment == "Cash On Delivery");

            // Customers
            ViewBag.TotalCustomers = orders
                .Select(x => x.UserId)
                .Distinct()
                .Count();

            ViewBag.RepeatCustomers = orders
                .GroupBy(x => x.UserId)
                .Count(g => g.Count() > 1);

            // Bids
            ViewBag.TotalBids = bids.Count;
            ViewBag.UniqueBidders = bids
                .Select(x => x.UserId)
                .Distinct()
                .Count();

            ViewBag.HighestBid = bids.Any()
                ? bids.Max(x => x.bidamount)
                : 0;

            ViewBag.LowestBid = bids.Any()
                ? bids.Min(x => x.bidamount)
                : 0;

            ViewBag.AverageBid = bids.Any()
                ? bids.Average(x => x.bidamount)
                : 0;

            // Dates
            if (orders.Any())
            {
                ViewBag.FirstSale = orders.Min(x => x.OrderDate);
                ViewBag.LastSale = orders.Max(x => x.OrderDate);
            }


            var now = DateTime.Now;

            // Seller's products
            var sellerProducts = bridge.products
                .Where(p => p.UserId == userId)
                .ToList();

            var sellerProductIds = sellerProducts.Select(p => p.Id).ToList();

            // Orders received for seller's products
            var sales = bridge.orders
                .Include(o => o.Product)
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Where(o => sellerProductIds.Contains(o.ProductId))
                .ToList();

            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalRevenue = sales.Sum(x => x.PricePaid);
            ViewBag.TotalItemsSold = sales.Sum(x => x.Quantity);


            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

            ViewBag.ThisWeekSales = sales.Count(x => x.OrderDate >= weekStart);

            ViewBag.ThisWeekRevenue = sales
                .Where(x => x.OrderDate >= weekStart)
                .Sum(x => x.PricePaid);

            var lastMonth = now.AddMonths(-1);

            ViewBag.LastMonthSales = sales.Count(x =>
                x.OrderDate.Month == lastMonth.Month &&
                x.OrderDate.Year == lastMonth.Year);

            ViewBag.LastMonthRevenue = sales
                .Where(x => x.OrderDate.Month == lastMonth.Month &&
                            x.OrderDate.Year == lastMonth.Year)
                .Sum(x => x.PricePaid);

            ViewBag.TotalProducts = sellerProducts.Count;
            ViewBag.AvailableProducts = sellerProducts.Count(x => x.quantity > 0);
            ViewBag.OutOfStock = sellerProducts.Count(x => x.quantity == 0);
            ViewBag.TotalStock = sellerProducts.Sum(x => x.quantity);


            ViewBag.TotalProducts = sellerProducts.Count;
            ViewBag.AvailableProducts = sellerProducts.Count(x => x.quantity > 0);
            ViewBag.OutOfStock = sellerProducts.Count(x => x.quantity == 0);
            ViewBag.TotalStock = sellerProducts.Sum(x => x.quantity);

            ViewBag.PendingSales = sales.Count(x => x.Status == "Pending");
            ViewBag.ProcessingSales = sales.Count(x => x.Status == "Processing");
            ViewBag.DispatchedSales = sales.Count(x => x.Status == "Dispatched");
            ViewBag.DeliveredSales = sales.Count(x => x.Status == "Delivered");
            ViewBag.CancelledSales = sales.Count(x => x.Status == "Cancelled");

            ViewBag.TotalCustomers = sales
    .Select(x => x.UserId)
    .Distinct()
    .Count();

            ViewBag.RepeatCustomers = sales
                .GroupBy(x => x.UserId)
                .Count(x => x.Count() > 1);


            var sellerBids = bridge.auctionDetails
    .Include(b => b.Product)
    .Include(b => b.User)
    .Where(b => sellerProductIds.Contains(b.ProductId))
    .ToList();

            ViewBag.TotalBidsReceived = sellerBids.Count;
            ViewBag.UniqueBidders = sellerBids.Select(x => x.UserId).Distinct().Count();
            ViewBag.HighestBid = sellerBids.Any() ? sellerBids.Max(x => x.bidamount) : 0;
            ViewBag.AverageBid = sellerBids.Any() ? sellerBids.Average(x => x.bidamount) : 0;

            return View();
        }



        public IActionResult Userallbids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var Userbids = bridge.auctionDetails
                .Include(b => b.Product)
                    .ThenInclude(p => p.SubCategory)
                .Include(b => b.User)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.Id)
                .ToList();

            return View(Userbids);
        }

        public IActionResult Userbiddetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var Userbiddetails = bridge.auctionDetails
                .Include(b => b.User)
                .Include(b => b.Product)
                    .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.category)
                .Include(b => b.Product)
                    .ThenInclude(p => p.User) // Seller
                .FirstOrDefault(b => b.Id == id && b.UserId == userId);



            var previousBids = bridge.auctionDetails
                .Include(b => b.User)
                .Where(b => b.ProductId == Userbiddetails.ProductId)
                .OrderByDescending(b => b.bidamount)
                .ToList();

            ViewBag.PreviousBids = previousBids;

            ViewBag.HighestBid = previousBids.Any()
                ? previousBids.Max(x => x.bidamount)
                : 0;

            ViewBag.TotalBids = previousBids.Count;

            ViewBag.YourRank = previousBids
                .OrderByDescending(x => x.bidamount)
                .ToList()
                .FindIndex(x => x.Id == Userbiddetails.Id) + 1;

            ViewBag.IsHighestBidder = previousBids.Any() &&
                                      previousBids.First().Id == Userbiddetails.Id;

            return View(Userbiddetails);
        }



        public IActionResult Adduserfeedback()
        {
            return View();
        }

        public IActionResult Adduserfeedbacklogic(Feedback feedback)
        {
            feedback.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bridge.feedbacks.Add(feedback);
            bridge.SaveChanges();

            
            TempData["Message"] = "Thanks alot  for giving your feedback.";



            return RedirectToAction("Adduserfeedback");

        }
        
        public IActionResult Allusersfeedbacks()
        {
            var Usersfeedbacks = bridge.feedbacks
       .Include(f => f.User)
       .ToList();
            return View(Usersfeedbacks);
        }

        public IActionResult Edituserfeedback(int id)
        {
            var feedback = bridge.feedbacks.Find(id);
            return View(feedback);
        }

        public IActionResult Edituserfeedbacklogic(int id, String message)
        {
            var Feedback = bridge.feedbacks.Find(id);

            Feedback.message = message;

            bridge.SaveChanges();
            TempData["Message"] = " feedback  updated sucessfully";

            return RedirectToAction("Allusersfeedbacks");
        }


        public IActionResult Deleteuserfeebacklogic(int id) {


            var Feedbackid = bridge.feedbacks.Find(id);
            bridge.feedbacks.Remove(Feedbackid);
            bridge.SaveChanges();

            TempData["Message"] = "User feedback  deleted sucessfully";


            return RedirectToAction("Allusersfeedbacks");
        }

        public IActionResult Addproductreview(int productid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = bridge.orders
                .Include(o => o.Product)
                    .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.category)
                .Include(o => o.Product)
                    .ThenInclude(p => p.User) // Seller
                .Include(o => o.User) // Buyer
                .FirstOrDefault(o =>
                    o.ProductId == productid &&
                    o.UserId == userId &&
                    o.Status == "Delivered");

       

            var productmodel = new ProductReview
            {
                ProductId = order.ProductId,
                Product = order.Product,
                UserId = userId,
            };

            return View(productmodel);
        }

        [HttpPost]
        public IActionResult Addproductreviewlogic(int productId, float ratings, string reviewMessage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var deliveredOrder = bridge.orders
                .FirstOrDefault(o =>
                    o.ProductId == productId &&
                    o.UserId == userId &&
                    o.Status == "Delivered");

            if (deliveredOrder == null)
            {
                TempData["Message"] = "You cannot review this product.";
                return RedirectToAction("Myorders");
            }

            var alreadyReviewed = bridge.productReviews
                .Any(r => r.ProductId == productId && r.UserId == userId);

            if (alreadyReviewed)
            {
                TempData["Message"] = "You have already reviewed this product.";
                return RedirectToAction("Myorders");
            }

            ProductReview review = new ProductReview()
            {
                ProductId = productId,
                UserId = userId,
                Ratings = ratings,
                ReviewMessage = reviewMessage
            };

            bridge.productReviews.Add(review);
            bridge.SaveChanges();

            TempData["Message"] = "Review submitted successfully.";

            return RedirectToAction("Myorders");
        }


        



    }
}