using Art_Gallery.Areas.Identity.Data;
using Art_Gallery.Data;
using Art_Gallery.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using System.Security.Claims;
using System.Text.Json; 

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Art_Gallery.Controllers
{
    
    public class AdminController : Controller
    {
        Art_GalleryContext bridge;
        private readonly IEmailSender _emailSender;
        private readonly IPasswordHasher<Art_GalleryUser> _passwordHasher;

        public AdminController(Art_GalleryContext _bridge, IEmailSender emailSender, IPasswordHasher<Art_GalleryUser> passwordHasher)
        {
            bridge = _bridge;
            _emailSender = emailSender;
            _passwordHasher = passwordHasher;
        }

        [Authorize]
public IActionResult Index()
    {
        var now = DateTime.Now;
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        var allOrders = bridge.orders
            .Include(o => o.Product)
                .ThenInclude(p => p.SubCategory)
                    .ThenInclude(sc => sc.category)
            .Include(o => o.Product)
                .ThenInclude(p => p.User)
            .Include(o => o.Payment)
            .Include(o => o.User)
            .ToList();

        // ---------- Congratulations / hero: today vs yesterday revenue ----------
        decimal todayRevenue = allOrders.Where(o => o.OrderDate.Date == today).Sum(o => o.PricePaid);
        decimal yesterdayRevenue = allOrders.Where(o => o.OrderDate.Date == yesterday).Sum(o => o.PricePaid);

        double? todayGrowthPercent = yesterdayRevenue > 0
            ? Math.Round((double)((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100, 1)
            : (todayRevenue > 0 ? 100 : (double?)null);

        var topSellerToday = allOrders
            .Where(o => o.OrderDate.Date == today && o.Product?.User != null)
            .GroupBy(o => o.Product.User.UserName)
            .Select(g => new { Seller = g.Key, Revenue = g.Sum(x => x.PricePaid) })
            .OrderByDescending(x => x.Revenue)
            .FirstOrDefault();

        ViewBag.TodayGrowthPercent = todayGrowthPercent;
        ViewBag.TopSellerToday = topSellerToday?.Seller;

        // ---------- Profit / Sales cards ----------
        decimal totalRevenue = allOrders.Sum(o => o.PricePaid);
        int totalOrdersCount = allOrders.Count;

        var thisMonthRevenue = allOrders.Where(o => o.OrderDate.Month == now.Month && o.OrderDate.Year == now.Year).Sum(o => o.PricePaid);
        var lastMonthDate = now.AddMonths(-1);
        var lastMonthRevenue = allOrders.Where(o => o.OrderDate.Month == lastMonthDate.Month && o.OrderDate.Year == lastMonthDate.Year).Sum(o => o.PricePaid);
        double? revenueMonthGrowth = lastMonthRevenue > 0
            ? Math.Round((double)((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1)
            : (thisMonthRevenue > 0 ? 100 : (double?)null);

        var thisMonthOrders = allOrders.Count(o => o.OrderDate.Month == now.Month && o.OrderDate.Year == now.Year);
        var lastMonthOrders = allOrders.Count(o => o.OrderDate.Month == lastMonthDate.Month && o.OrderDate.Year == lastMonthDate.Year);
        double? ordersMonthGrowth = lastMonthOrders > 0
            ? Math.Round(((double)(thisMonthOrders - lastMonthOrders) / lastMonthOrders) * 100, 1)
            : (thisMonthOrders > 0 ? 100 : (double?)null);

        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.TotalOrdersCount = totalOrdersCount;
        ViewBag.RevenueMonthGrowth = revenueMonthGrowth;
        ViewBag.OrdersMonthGrowth = ordersMonthGrowth;

        // ---------- Total Revenue chart: monthly totals, up to last 3 years ----------
        var years = allOrders.Select(o => o.OrderDate.Year).Distinct().OrderByDescending(y => y).ToList();
        if (!years.Contains(now.Year)) years.Insert(0, now.Year);
        years = years.Take(3).ToList();

        var monthlyRevenueByYear = new Dictionary<int, List<decimal>>();
        foreach (var y in years)
        {
            var monthly = new List<decimal>();
            for (int m = 1; m <= 12; m++)
                monthly.Add(allOrders.Where(o => o.OrderDate.Year == y && o.OrderDate.Month == m).Sum(o => o.PricePaid));
            monthlyRevenueByYear[y] = monthly;
        }

        decimal currentYearTotal = monthlyRevenueByYear.ContainsKey(now.Year) ? monthlyRevenueByYear[now.Year].Sum() : 0;
        decimal previousYearTotal = monthlyRevenueByYear.ContainsKey(now.Year - 1) ? monthlyRevenueByYear[now.Year - 1].Sum() : 0;
        double? companyGrowthPercent = previousYearTotal > 0
            ? Math.Round((double)((currentYearTotal - previousYearTotal) / previousYearTotal) * 100, 1)
            : (double?)null;

        ViewBag.RevenueYears = years;
        ViewBag.MonthlyRevenueByYearJson = JsonSerializer.Serialize(
            years.ToDictionary(y => y, y => monthlyRevenueByYear[y]));
        ViewBag.CompanyGrowthPercent = companyGrowthPercent;
        ViewBag.CurrentYearRevenue = currentYearTotal;
        ViewBag.PreviousYearRevenue = previousYearTotal;
        ViewBag.SelectedYear = years.First();

        // ---------- Order Statistics: category breakdown (by quantity) ----------
        var categoryBreakdown = allOrders
            .Where(o => o.Product?.SubCategory?.category != null)
            .GroupBy(o => o.Product.SubCategory.category.Name)
            .Select(g => new { Category = g.Key, Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Qty)
            .Take(4)
            .ToList();

        ViewBag.CategoryBreakdown = categoryBreakdown;
        ViewBag.CategoryQuantitiesJson = JsonSerializer.Serialize(categoryBreakdown.Select(x => x.Qty));
        ViewBag.CategoryNamesJson = JsonSerializer.Serialize(categoryBreakdown.Select(x => x.Category));
        ViewBag.TotalItemsSoldOverall = allOrders.Sum(o => o.Quantity);

        // ---------- Weekly Income chart (last 7 days) ----------
        var weeklyLabels = new List<string>();
        var weeklyRevenue = new List<decimal>();
        for (int d = 6; d >= 0; d--)
        {
            var day = today.AddDays(-d);
            weeklyLabels.Add(day.ToString("ddd"));
            weeklyRevenue.Add(allOrders.Where(o => o.OrderDate.Date == day).Sum(o => o.PricePaid));
        }
        decimal thisWeekTotal = weeklyRevenue.Sum();
        decimal priorWeekTotal = allOrders
            .Where(o => o.OrderDate.Date >= today.AddDays(-14) && o.OrderDate.Date < today.AddDays(-7))
            .Sum(o => o.PricePaid);

        ViewBag.WeeklyLabelsJson = JsonSerializer.Serialize(weeklyLabels);
        ViewBag.WeeklyRevenueJson = JsonSerializer.Serialize(weeklyRevenue);
        ViewBag.WeeklyRevenueTotal = thisWeekTotal;
        ViewBag.WeeklyRevenueDiff = thisWeekTotal - priorWeekTotal;

        // ---------- Recent Transactions ----------
        ViewBag.RecentTransactions = allOrders
            .OrderByDescending(o => o.OrderDate)
            .Take(6)
            .Select(o => new
            {
                Method = o.Payment?.ModeofPayment ?? "N/A",
                ProductName = o.Product?.Name ?? "Deleted product",
                Amount = o.PricePaid,
                Date = o.OrderDate
            })
            .ToList();

        // ---------- Profile Report: product approval status ----------
        var allProducts = bridge.products.ToList();
        int totalProducts = allProducts.Count;
        int approvedProducts = allProducts.Count(p => p.Status == "Available");
        ViewBag.ApprovedProductsPercent = totalProducts > 0 ? Math.Round(((double)approvedProducts / totalProducts) * 100, 1) : 0;
        ViewBag.PendingProductsCount = allProducts.Count(p => p.Status == "Pending");
        ViewBag.TotalProductsCount = totalProducts;

        ViewBag.TotalCustomers = bridge.Users.Count();

        // ---------- Catalog counts ----------
        ViewBag.TotalCategories = bridge.categories.Count();
        ViewBag.TotalSubCategories = bridge.subCategories.Count();
        ViewBag.AvailableProductsCount = allProducts.Count(p => p.Status == "Available");
        ViewBag.RejectedProductsCount = allProducts.Count(p => p.Status == "Rejected");

        // ---------- Engagement ----------
        ViewBag.TotalWishlistItems = bridge.wishlist.Count();
        ViewBag.TotalFeedbacks = bridge.feedbacks.Count();
        ViewBag.TotalContacts = bridge.contacts.Count();
        ViewBag.TotalPaymentMethodsSaved = bridge.paymentDetails.Count();

        var allReviews = bridge.productReviews.ToList();
        ViewBag.TotalProductReviews = allReviews.Count;
        ViewBag.AverageRating = allReviews.Any() ? Math.Round(allReviews.Average(r => r.Ratings), 1) : 0;

        // ---------- Auctions / bids ----------
        var allBids = bridge.auctionDetails.ToList();
        ViewBag.TotalBids = allBids.Count;
        ViewBag.ActiveAuctions = allProducts.Count(p => p.AvailableForBid == "Yes" && p.Status == "Available");
        ViewBag.HighestBidAmount = allBids.Any() ? allBids.Max(b => b.bidamount) : 0;
        ViewBag.PendingBids = allBids.Count(b => b.bidstatus == "Pending");

        // ---------- Order status breakdown ----------
        ViewBag.OrdersPending = allOrders.Count(o => o.Status == "Pending");
        ViewBag.OrdersProcessing = allOrders.Count(o => o.Status == "Processing");
        ViewBag.OrdersDispatched = allOrders.Count(o => o.Status == "Dispatched");
        ViewBag.OrdersDelivered = allOrders.Count(o => o.Status == "Delivered");
        ViewBag.OrdersCancelled = allOrders.Count(o => o.Status == "Cancelled");
        ViewBag.OrdersRejected = allOrders.Count(o => o.Status == "Rejected");

        // ---------- Top performers ----------
        ViewBag.TopProducts = allOrders
            .Where(o => o.Product != null)
            .GroupBy(o => new { o.ProductId, o.Product.Name })
            .Select(g => new { g.Key.Name, Revenue = g.Sum(x => x.PricePaid), Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        ViewBag.TopCustomers = allOrders
            .Where(o => o.User != null)
            .GroupBy(o => new { o.UserId, o.User.UserName })
            .Select(g => new { g.Key.UserName, Spent = g.Sum(x => x.PricePaid), Orders = g.Count() })
            .OrderByDescending(x => x.Spent)
            .Take(5)
            .ToList();

        ViewBag.TopSellers = allOrders
            .Where(o => o.Product != null && o.Product.User != null)
            .GroupBy(o => new { o.Product.UserId, o.Product.User.UserName })
            .Select(g => new { g.Key.UserName, Revenue = g.Sum(x => x.PricePaid), Sales = g.Count() })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        // ---------- Payment methods ----------
        var paymentBreakdown = allOrders
            .Where(o => o.Payment != null)
            .GroupBy(o => o.Payment.ModeofPayment)
            .Select(g => new { Method = g.Key, Count = g.Count(), Revenue = g.Sum(x => x.PricePaid) })
            .ToList();
        ViewBag.PaymentBreakdown = paymentBreakdown;
        ViewBag.PaymentMethodCountsJson = JsonSerializer.Serialize(paymentBreakdown.Select(p => p.Count));
        ViewBag.PaymentMethodLabelsJson = JsonSerializer.Serialize(paymentBreakdown.Select(p => p.Method));

        // ---------- Category revenue ----------
        ViewBag.CategoryRevenue = allOrders
            .Where(o => o.Product != null && o.Product.SubCategory != null && o.Product.SubCategory.category != null)
            .GroupBy(o => o.Product.SubCategory.category.Name)
            .Select(g => new { Category = g.Key, Revenue = g.Sum(x => x.PricePaid) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        // ---------- Recent reviews ----------
        ViewBag.RecentReviews = bridge.productReviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .OrderByDescending(r => r.Id)
            .Take(5)
            .Select(r => new { Product = r.Product.Name, User = r.User.UserName, r.Ratings, r.ReviewMessage })
            .ToList();

        // ---------- Recent feedbacks ----------
        ViewBag.RecentFeedbacks = bridge.feedbacks
            .Include(f => f.User)
            .OrderByDescending(f => f.Id)
            .Take(5)
            .Select(f => new { User = f.User.UserName, f.message })
            .ToList();

        // ---------- User demographics ----------
        var allUsers = bridge.Users.ToList();
        ViewBag.MaleCount = allUsers.Count(u => u.gender == "Male");
        ViewBag.FemaleCount = allUsers.Count(u => u.gender == "Female");
        ViewBag.OtherGenderCount = allUsers.Count(u => u.gender != "Male" && u.gender != "Female");
        ViewBag.AverageAge = allUsers.Any(u => u.age > 0)
            ? Math.Round(allUsers.Where(u => u.age > 0).Average(u => u.age), 1)
            : 0;

        // ---------- Top bids ----------
        ViewBag.TopBids = bridge.auctionDetails
            .Include(b => b.Product)
            .Include(b => b.User)
            .OrderByDescending(b => b.bidamount)
            .Take(5)
            .Select(b => new { Product = b.Product.Name, Bidder = b.User.UserName, b.bidamount, b.bidstatus })
            .ToList();

        return View();
    }
        // all user work started from here 

        // function to display all user on AllCustomers page
        [Authorize]
        public IActionResult AllCustomers()
        {
            return View(bridge.Users.ToList());
        }

        [Authorize]
        public IActionResult Customerdetails(string id)
        {
            var user = bridge.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var now = DateTime.Now;

            // ================= BUYER HISTORY =================
            var orders = bridge.orders
                .Include(o => o.Product)
                    .ThenInclude(p => p.SubCategory)
                        .ThenInclude(sc => sc.category)
                .Include(o => o.Payment)
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var wishlist = bridge.wishlist
                .Include(w => w.Product)
                .Where(w => w.UserId == id)
                .ToList();

            var bidsPlaced = bridge.auctionDetails
                .Include(b => b.Product)
                    .ThenInclude(p => p.SubCategory)
                .Where(b => b.UserId == id)
                .OrderByDescending(b => b.Id)
                .ToList();

            var paymentMethods = bridge.paymentDetails
                .Where(p => p.UserId == id)
                .ToList();

            var reviewsGiven = bridge.productReviews
                .Include(r => r.Product)
                .Where(r => r.UserId == id)
                .ToList();

            var feedbacksGiven = bridge.feedbacks
                .Where(f => f.UserId == id)
                .ToList();

            var contactsSent = bridge.contacts
                .Where(c => c.UserId == id)
                .ToList();

            ViewBag.Orders = orders;
            ViewBag.Wishlist = wishlist;
            ViewBag.BidsPlaced = bidsPlaced;
            ViewBag.PaymentMethods = paymentMethods;
            ViewBag.ReviewsGiven = reviewsGiven;
            ViewBag.FeedbacksGiven = feedbacksGiven;
            ViewBag.ContactsSent = contactsSent;

            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalWishlist = wishlist.Count;
            ViewBag.TotalBidsPlaced = bidsPlaced.Count;
            ViewBag.PendingOrders = orders.Count(x => x.Status == "Pending");
            ViewBag.ProcessingOrders = orders.Count(x => x.Status == "Processing");
            ViewBag.DispatchedOrders = orders.Count(x => x.Status == "Dispatched");
            ViewBag.DeliveredOrders = orders.Count(x => x.Status == "Delivered");
            ViewBag.CancelledOrders = orders.Count(x => x.Status == "Cancelled");
            ViewBag.RejectedOrders = orders.Count(x => x.Status == "Rejected");
            ViewBag.TotalItemsPurchased = orders.Sum(x => x.Quantity);
            ViewBag.TotalMoneySpent = orders.Sum(x => x.PricePaid);
            ViewBag.AverageOrderValue = orders.Count > 0 ? orders.Average(x => x.PricePaid) : 0;
            ViewBag.HighestOrder = orders.Count > 0 ? orders.Max(x => x.PricePaid) : 0;
            ViewBag.LowestOrder = orders.Count > 0 ? orders.Min(x => x.PricePaid) : 0;
            ViewBag.CODOrders = orders.Count(x => x.Payment != null && x.Payment.ModeofPayment == "Cash On Delivery");
            ViewBag.CardOrders = orders.Count(x => x.Payment != null && x.Payment.ModeofPayment == "Card");
            ViewBag.UniqueProductsPurchased = orders.Select(x => x.ProductId).Distinct().Count();
            ViewBag.FirstOrderDate = orders.Any() ? (DateTime?)orders.Min(x => x.OrderDate) : null;
            ViewBag.LastOrderDate = orders.Any() ? (DateTime?)orders.Max(x => x.OrderDate) : null;
            ViewBag.HighestBidPlaced = bidsPlaced.Any() ? bidsPlaced.Max(b => b.bidamount) : 0;
            ViewBag.WonBidsCount = bidsPlaced.Count(b => b.bidstatus == "Won");
            ViewBag.AverageRatingGiven = reviewsGiven.Any() ? Math.Round(reviewsGiven.Average(r => r.Ratings), 1) : 0;

            var monthlySpending = orders
                .Where(o => o.OrderDate >= now.AddMonths(-5).Date)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Total = g.Sum(x => x.PricePaid)
                })
                .ToList();

            ViewBag.MonthlySpendingLabelsJson = JsonSerializer.Serialize(monthlySpending.Select(x => x.Label));
            ViewBag.MonthlySpendingValuesJson = JsonSerializer.Serialize(monthlySpending.Select(x => x.Total));

            var topProducts = orders
                .Where(o => o.Product != null)
                .GroupBy(o => o.Product.Name)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToList();

            ViewBag.TopProductNamesJson = JsonSerializer.Serialize(topProducts.Select(x => x.Name));
            ViewBag.TopProductQtyJson = JsonSerializer.Serialize(topProducts.Select(x => x.Qty));

            // ================= SELLER HISTORY =================
            var sellerProducts = bridge.products
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.category)
                .Where(p => p.UserId == id)
                .ToList();

            var sellerProductIds = sellerProducts.Select(p => p.Id).ToList();

            var sales = bridge.orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .Include(o => o.Payment)
                .Where(o => sellerProductIds.Contains(o.ProductId))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var bidsReceived = bridge.auctionDetails
                .Include(b => b.User)
                .Include(b => b.Product)
                .Where(b => sellerProductIds.Contains(b.ProductId))
                .OrderByDescending(b => b.Id)
                .ToList();

            var reviewsReceived = bridge.productReviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => sellerProductIds.Contains(r.ProductId))
                .ToList();

            ViewBag.IsSeller = sellerProducts.Any();
            ViewBag.SellerProducts = sellerProducts;
            ViewBag.Sales = sales;
            ViewBag.BidsReceived = bidsReceived;
            ViewBag.ReviewsReceived = reviewsReceived;

            ViewBag.TotalProductsListed = sellerProducts.Count;
            ViewBag.AvailableProducts = sellerProducts.Count(x => x.Status == "Available");
            ViewBag.PendingProducts = sellerProducts.Count(x => x.Status == "Pending");
            ViewBag.RejectedProducts = sellerProducts.Count(x => x.Status == "Rejected");
            ViewBag.OutOfStockProducts = sellerProducts.Count(x => x.quantity == 0);

            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalRevenue = sales.Sum(x => x.PricePaid);
            ViewBag.TotalItemsSold = sales.Sum(x => x.Quantity);
            ViewBag.AverageSaleValue = sales.Any() ? sales.Average(x => x.PricePaid) : 0;
            ViewBag.HighestSale = sales.Any() ? sales.Max(x => x.PricePaid) : 0;
            ViewBag.PendingSales = sales.Count(x => x.Status == "Pending");
            ViewBag.ProcessingSales = sales.Count(x => x.Status == "Processing");
            ViewBag.DispatchedSales = sales.Count(x => x.Status == "Dispatched");
            ViewBag.DeliveredSales = sales.Count(x => x.Status == "Delivered");
            ViewBag.CancelledSales = sales.Count(x => x.Status == "Cancelled");

            ViewBag.TotalCustomersServed = sales.Select(x => x.UserId).Distinct().Count();
            ViewBag.RepeatCustomers = sales.GroupBy(x => x.UserId).Count(g => g.Count() > 1);

            ViewBag.TotalBidsReceived = bidsReceived.Count;
            ViewBag.UniqueBidders = bidsReceived.Select(x => x.UserId).Distinct().Count();
            ViewBag.HighestBidReceived = bidsReceived.Any() ? bidsReceived.Max(x => x.bidamount) : 0;
            ViewBag.PendingBidsReceived = bidsReceived.Count(b => b.bidstatus == "Pending");

            ViewBag.TotalReviewsReceived = reviewsReceived.Count;
            ViewBag.AverageRatingReceived = reviewsReceived.Any() ? Math.Round(reviewsReceived.Average(r => r.Ratings), 1) : 0;

            var monthlyRevenue = sales
                .Where(o => o.OrderDate >= now.AddMonths(-5).Date)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Total = g.Sum(x => x.PricePaid)
                })
                .ToList();

            ViewBag.MonthlyRevenueLabelsJson = JsonSerializer.Serialize(monthlyRevenue.Select(x => x.Label));
            ViewBag.MonthlyRevenueValuesJson = JsonSerializer.Serialize(monthlyRevenue.Select(x => x.Total));
            // ================= SELLER — DEEPER ANALYTICS =================

            // Per-product performance
            var perProductStats = sellerProducts.Select(p => new
            {
                Product = p,
                UnitsSold = sales.Where(s => s.ProductId == p.Id).Sum(s => s.Quantity),
                Revenue = sales.Where(s => s.ProductId == p.Id).Sum(s => s.PricePaid),
                OrdersCount = sales.Count(s => s.ProductId == p.Id),
                BidsCount = bidsReceived.Count(b => b.ProductId == p.Id),
                HighestBid = bidsReceived.Where(b => b.ProductId == p.Id).Any()
                                ? bidsReceived.Where(b => b.ProductId == p.Id).Max(b => b.bidamount)
                                : 0,
                AvgRating = reviewsReceived.Where(r => r.ProductId == p.Id).Any()
                                ? Math.Round(reviewsReceived.Where(r => r.ProductId == p.Id).Average(r => r.Ratings), 1)
                                : 0
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

            ViewBag.PerProductStats = perProductStats;

            // Category breakdown of this seller's catalog
            var sellerCategoryBreakdown = sellerProducts
                .Where(p => p.SubCategory?.category != null)
                .GroupBy(p => p.SubCategory.category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    ProductCount = g.Count(),
                    Revenue = sales.Where(s => g.Select(x => x.Id).Contains(s.ProductId)).Sum(s => s.PricePaid)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            ViewBag.SellerCategoryNamesJson = JsonSerializer.Serialize(sellerCategoryBreakdown.Select(x => x.Category));
            ViewBag.SellerCategoryRevenueJson = JsonSerializer.Serialize(sellerCategoryBreakdown.Select(x => x.Revenue));
            ViewBag.SellerCategoryBreakdown = sellerCategoryBreakdown;

            // Top buyers of this seller's products
            var topBuyers = sales
                .Where(s => s.User != null)
                .GroupBy(s => new { s.UserId, s.User.UserName })
                .Select(g => new
                {
                    g.Key.UserName,
                    OrdersCount = g.Count(),
                    Revenue = g.Sum(x => x.PricePaid)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            ViewBag.TopBuyers = topBuyers;

            // Combined recent activity feed (sales + bids), most recent first
            var recentActivity = sales.Select(s => new
            {
                Type = "Sale",
                Description = $"{s.User?.UserName ?? "N/A"} bought {s.Product?.Name ?? "a product"} (x{s.Quantity})",
                Amount = s.PricePaid,
                Date = s.OrderDate
            })
                .Concat(bidsReceived.Select(b => new
                {
                    Type = "Bid",
                    Description = $"{b.User?.UserName ?? "N/A"} bid on {b.Product?.Name ?? "a product"}",
                    Amount = (decimal)b.bidamount,
                    Date = DateTime.Now // AuctionDetails has no date field; swap in a real timestamp if you add one
                }))
                .OrderByDescending(x => x.Date)
                .Take(10)
                .ToList();

            ViewBag.RecentActivity = recentActivity;
            return View(user);
        }



        //function to delete a Customer completely from the database all its products wishlist items payment details



        [Authorize]
        public async Task<IActionResult> Deletecustomerlogic(string id)
        {
            var user = bridge.Users.Find(id);

            if (user == null)
                return NotFound();

            // Read the email template
            var accountDeletedBody = System.IO.File.ReadAllText("Views/Emails/Accountdeletedemail.html");
            accountDeletedBody = accountDeletedBody.Replace("{{Username}}", user.UserName);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Your ArtGallery Account Has Been Removed",
                accountDeletedBody);

            // Get all products of this user
            var userProductIds = bridge.products
                .Where(p => p.UserId == id)
                .Select(p => p.Id)
                .ToList();

            // Remove wishlists containing this user's products
            var wishlistForTheirProducts = bridge.wishlist
                .Where(w => userProductIds.Contains(w.ProductId));
            bridge.wishlist.RemoveRange(wishlistForTheirProducts);

            // Remove user's own wishlist
            var userWishlist = bridge.wishlist
                .Where(w => w.UserId == id);
            bridge.wishlist.RemoveRange(userWishlist);

            // Remove reviews written by user
            var userReviews = bridge.productReviews
                .Where(r => r.UserId == id);
            bridge.productReviews.RemoveRange(userReviews);

            // Remove reviews on user's products
            var productReviews = bridge.productReviews
                .Where(r => userProductIds.Contains(r.ProductId));
            bridge.productReviews.RemoveRange(productReviews);

            // Remove user's auction bids
            var userAuctions = bridge.auctionDetails
                .Where(a => a.UserId == id);
            bridge.auctionDetails.RemoveRange(userAuctions);

            // Remove auction records of user's products
            var productAuctions = bridge.auctionDetails
                .Where(a => userProductIds.Contains(a.ProductId));
            bridge.auctionDetails.RemoveRange(productAuctions);

            // Remove payments of user's orders
            var userOrders = bridge.orders
                .Where(o => o.UserId == id)
                .ToList();

            foreach (var order in userOrders)
            {
                var payment = bridge.payments
                    .FirstOrDefault(p => p.OrderId == order.Id);

                if (payment != null)
                    bridge.payments.Remove(payment);
            }

            // Remove payments of orders placed on user's products
            var productOrders = bridge.orders
                .Where(o => userProductIds.Contains(o.ProductId))
                .ToList();

            foreach (var order in productOrders)
            {
                var payment = bridge.payments
                    .FirstOrDefault(p => p.OrderId == order.Id);

                if (payment != null)
                    bridge.payments.Remove(payment);
            }

            // Remove user's orders
            bridge.orders.RemoveRange(userOrders);

            // Remove orders of user's products
            bridge.orders.RemoveRange(productOrders);

            // Remove payment details
            var paymentDetails = bridge.paymentDetails
                .Where(pd => pd.UserId == id);
            bridge.paymentDetails.RemoveRange(paymentDetails);

            // Remove user's products
            var userProducts = bridge.products
                .Where(p => p.UserId == id);
            bridge.products.RemoveRange(userProducts);

            // Finally remove user
            bridge.Users.Remove(user);

            await bridge.SaveChangesAsync();

            TempData["Message"] = "Customer and all related records were deleted successfully.";

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
        [Authorize]
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
        [Authorize]
        public IActionResult Allcategories()
        {
            return View(bridge.categories.ToList());

        }



        [Authorize]
        //function to open  edit category page with values in input fields 
        public IActionResult Editcategory(int id)
        {
            var category = bridge.categories.Find(id);
            return View(category);
        }

        // function to save the edited values in the database
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        public IActionResult Allsubcategories()
        {
            var data = bridge.subCategories
                      .Include(s => s.category)
                      .ToList();

            return View(data);
        }


        //function to open Editsubcategory page with values in the input fields
        [Authorize]
        public IActionResult Editsubcategory(int id)
        {
         var subcategory = bridge.subCategories
                           .Include(c => c.category) 
                           .FirstOrDefault(c => c.Id == id);
                            return View(subcategory);
        
        }
        // function to save the subcategory updated values int the database  
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        public IActionResult Addproduct()
        {
            ViewBag.Categories = bridge.categories.ToList();
            ViewBag.SubCategories = bridge.subCategories.ToList();
            return View();
        }

        // function to add a product in the products table
        [Authorize]
        public IActionResult Addproductlogic(
    string Name, string Description,
    List<IFormFile> Images,
    float price, int quantity, string AvailableForBid,
    DateOnly BidStartDate, DateOnly BidEndDate, float BidPrice,
    int SubCategoryId, string Status)
        {
            var validImages = (Images ?? new List<IFormFile>())
                .Where(f => f != null && f.Length > 0)
                .ToList();

            if (validImages.Count != 3)
            {
                TempData["Message"] = "Please upload all 3 images.";
                return RedirectToAction("Addproduct");
            }

            if (AvailableForBid == "Yes")
            {
                if (BidStartDate == default || BidEndDate == default || BidPrice <= 0)
                {
                    TempData["Message"] = "Please fill in bid start date, end date, and bid price for an auction.";
                    return RedirectToAction("Addproduct");
                }
                price = BidPrice;
                quantity = 1;
            }
            else
            {
                if (price <= 0)
                {
                    TempData["Message"] = "Please enter a valid price.";
                    return RedirectToAction("Addproduct");
                }
                BidStartDate = default;
                BidEndDate = default;
                BidPrice = 0;
            }

            if (string.IsNullOrWhiteSpace(Status))
            {
                TempData["Message"] = "Please select a status.";
                return RedirectToAction("Addproduct");
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var product = bridge.products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            // Delete Payments
            var orders = bridge.orders.Where(o => o.ProductId == id).ToList();
            foreach (var order in orders)
            {
                var payment = bridge.payments.FirstOrDefault(p => p.OrderId == order.Id);
                if (payment != null)
                {
                    bridge.payments.Remove(payment);
                }
            }
            bridge.SaveChanges();
            // Delete Orders
            bridge.orders.RemoveRange(orders);
            bridge.SaveChanges();
            // Delete Auction Details
            var auctions = bridge.auctionDetails
                .Where(a => a.ProductId == id)
                .ToList();
            bridge.auctionDetails.RemoveRange(auctions);
            bridge.SaveChanges();
            // Delete Reviews
            var reviews = bridge.productReviews
                .Where(r => r.ProductId == id)
                .ToList();
            bridge.productReviews.RemoveRange(reviews);
            bridge.SaveChanges();
            // Delete Product
            bridge.products.Remove(product);
            bridge.SaveChanges();
            TempData["Message"] = "Product deleted successfully.";
            return RedirectToAction("Allproducts");
        }

        // function to fetch all the products from product table with pending status
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> Acceptrequestlogic(int id)
        {
            var Product = bridge.products.Find(id);

            if (Product == null)
                return NotFound();

            var user = bridge.Users.Find(Product.UserId);

            Product.Status = "Available";

            await bridge.SaveChangesAsync();

            var body = System.IO.File.ReadAllText("Views/Emails/ProductApproved.html");

            body = body.Replace("{{Username}}", user.UserName);
            body = body.Replace("{{ProductName}}", Product.Name);
            body = body.Replace("{{WebsiteUrl}}", "https://localhost:7015");
            body = body.Replace("{{LogoUrl}}", "https://localhost:7015/images/logo.png");

            await _emailSender.SendEmailAsync(
                user.Email,
                "🎉 Your Product Has Been Approved!",
                body);

            TempData["Message"] = $"\"{Product.Name}\" has been approved and is now live.";

            return RedirectToAction("Productrequest");
        }
        // function to reject a project,rejecting a product will change its status from pending to rejecting making it never visible on all products page 
        [Authorize]
        public async Task<IActionResult> Rejectrequestlogic(int id)
        {
            var Product = bridge.products.Find(id);

            if (Product == null)
                return NotFound();

            var user = bridge.Users.Find(Product.UserId);

            Product.Status = "Rejected";

            await bridge.SaveChangesAsync();

            // Read email template
            var body = System.IO.File.ReadAllText("Views/Emails/ProductRejected.html");

            // Replace placeholders
            body = body.Replace("{{Username}}", user.UserName);
            body = body.Replace("{{ProductName}}", Product.Name);
            body = body.Replace("{{WebsiteUrl}}", "https://localhost:7015");
            body = body.Replace("{{LogoUrl}}", "https://localhost:7015/images/logo.png");

            // Send email
            await _emailSender.SendEmailAsync(
                user.Email,
                "❌ Your Product Has Been Rejected",
                body);

            TempData["Warningmessage"] = $"\"{Product.Name}\" has been rejected.";

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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        public IActionResult Allpaymentdetails()

        {
            var paymentdetails = bridge.paymentDetails
                               .Include(p => p.User)
                               .ToList();

            return View(paymentdetails);

        }

        //function to delete a user payment details completely from the paymentdetails table
        [Authorize]
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
        [Authorize]
        public IActionResult Adminproductsorders()
        {
            var orders = bridge.orders.Include(o => o.User)
    .Include(o => o.Product)
    .Include(o => o.Payment).ToList();
            return View(orders);
        }

        // function to display a detailed information related to order on Adminorderdetails page
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        public IActionResult Allcontacts()
        {
            var contacts = bridge.contacts.Include(c => c.User).ToList();
            return View(contacts);
        }


        //function to delete a user contact message completely from the database
        [Authorize]
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
        //Convert Reports to Excel
        public IActionResult ExportOrdersToExcel()
        {
            var orders = bridge.orders
                .Include(o => o.Payment)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Orders");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Wishlist ID";
                worksheet.Cell(1, 3).Value = "User ID";
                worksheet.Cell(1, 4).Value = "Product ID";
                worksheet.Cell(1, 5).Value = "Status";
                worksheet.Cell(1, 6).Value = "Quantity";
                worksheet.Cell(1, 7).Value = "Price Paid";
                worksheet.Cell(1, 8).Value = "Order Date";
                worksheet.Cell(1, 9).Value = "Shipping Address";
                worksheet.Cell(1, 10).Value = "Contact Phone";
                worksheet.Cell(1, 11).Value = "Payment";

                var headerRange = worksheet.Range("A1:K1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var o in orders)
                {
                    worksheet.Cell(row, 1).Value = o.Id;
                    worksheet.Cell(row, 2).Value = o.WishlistId;
                    worksheet.Cell(row, 3).Value = o.UserId;
                    worksheet.Cell(row, 4).Value = o.ProductId;
                    worksheet.Cell(row, 5).Value = o.Status;
                    worksheet.Cell(row, 6).Value = o.Quantity;
                    worksheet.Cell(row, 7).Value = o.PricePaid;
                    worksheet.Cell(row, 8).Value = o.OrderDate;
                    worksheet.Cell(row, 9).Value = o.ShippingAddress;
                    worksheet.Cell(row, 10).Value = o.ContactPhone;
                    worksheet.Cell(row, 11).Value = o.Payment?.OrderId;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Orders.xlsx"
                    );
                }
            }
        }

        public IActionResult ExportCategoriesToExcel()
        {
            var categories = bridge.categories.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Categories");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Category Image";

                var headerRange = worksheet.Range("A1:C1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var c in categories)
                {
                    worksheet.Cell(row, 1).Value = c.Id;
                    worksheet.Cell(row, 2).Value = c.Name;
                    worksheet.Cell(row, 3).Value = c.Categoryimage;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Categories.xlsx"
                    );
                }
            }
        }

        public IActionResult ExportSubCategoriesToExcel()
        {
            var subcategories = bridge.subCategories
                .Include(s => s.category)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All SubCategories");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Image";
                worksheet.Cell(1, 4).Value = "Category";

                var headerRange = worksheet.Range("A1:D1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var sub in subcategories)
                {
                    worksheet.Cell(row, 1).Value = sub.Id;
                    worksheet.Cell(row, 2).Value = sub.Name;
                    worksheet.Cell(row, 3).Value = sub.SubCategoryimage;
                    worksheet.Cell(row, 4).Value = sub.category?.Name;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "SubCategories.xlsx"
                    );
                }
            }
        }

        public IActionResult ExportProductsToExcel()
        {
            var products = bridge.products
                .Include(p => p.SubCategory)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Products");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Description";
                worksheet.Cell(1, 4).Value = "Image";
                worksheet.Cell(1, 5).Value = "Price";
                worksheet.Cell(1, 6).Value = "Quantity";
                worksheet.Cell(1, 7).Value = "Sub Category Name";
                worksheet.Cell(1, 8).Value = "Status";
                worksheet.Cell(1, 9).Value = "Available for Bid";
                worksheet.Cell(1, 10).Value = "Bid Price";

                var headerRange = worksheet.Range("A1:J1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var p in products)
                {
                    worksheet.Cell(row, 1).Value = p.Id;
                    worksheet.Cell(row, 2).Value = p.Name;
                    worksheet.Cell(row, 3).Value = p.Description;
                    worksheet.Cell(row, 4).Value = p.Image1;
                    worksheet.Cell(row, 5).Value = p.price;
                    worksheet.Cell(row, 6).Value = p.quantity;
                    worksheet.Cell(row, 7).Value = p.SubCategory?.Name;
                    worksheet.Cell(row, 8).Value = p.Status;
                    worksheet.Cell(row, 9).Value = p.AvailableForBid;
                    worksheet.Cell(row, 10).Value = p.BidPrice;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Products.xlsx"
                    );
                }
            }
        }

        public IActionResult ExportAuctionsToExcel()
        {
            var auctiondetails = bridge.auctionDetails
                .Include(a => a.Product)
                .Include(a => a.User)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Auctions");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Product";
                worksheet.Cell(1, 3).Value = "User";
                worksheet.Cell(1, 4).Value = "Bid Amount";
                worksheet.Cell(1, 5).Value = "Bid Status";

                var headerRange = worksheet.Range("A1:E1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var a in auctiondetails)
                {
                    worksheet.Cell(row, 1).Value = a.Id;
                    worksheet.Cell(row, 2).Value = a.Product?.Name;
                    worksheet.Cell(row, 3).Value = a.User?.UserName;
                    worksheet.Cell(row, 4).Value = a.bidamount;
                    worksheet.Cell(row, 5).Value = a.bidstatus;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "AuctionDetails.xlsx"
                    );
                }
            }
        }

        public IActionResult ExportPaymentsToExcel()
        {
            var payments = bridge.payments
                .Include(p => p.Order)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Payments");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Mode of Payment";
                worksheet.Cell(1, 3).Value = "Order Status";

                var headerRange = worksheet.Range("A1:C1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var p in payments)
                {
                    worksheet.Cell(row, 1).Value = p.Id;
                    worksheet.Cell(row, 2).Value = p.ModeofPayment;
                    worksheet.Cell(row, 3).Value = p.Order?.Status;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Payments.xlsx"
                    );
                }
            }
        }

        public IActionResult ExportContactsToExcel()
        {
            var contacts = bridge.contacts
                .Include(c => c.User)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Contacts");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Message";
                worksheet.Cell(1, 3).Value = "User";

                var headerRange = worksheet.Range("A1:C1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var c in contacts)
                {
                    worksheet.Cell(row, 1).Value = c.Id;
                    worksheet.Cell(row, 2).Value = c.Message;
                    worksheet.Cell(row, 3).Value = c.User?.UserName;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Contacts.xlsx"
                    );
                }
            }
        }

        public IActionResult ExportCustomersToExcel()
        {
            var users = bridge.Users.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Users");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Address";
                worksheet.Cell(1, 4).Value = "Gender";
                worksheet.Cell(1, 5).Value = "Age";
                worksheet.Cell(1, 6).Value = "Role";
                worksheet.Cell(1, 7).Value = "Email";

                var headerRange = worksheet.Range("A1:G1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var u in users)
                {
                    worksheet.Cell(row, 1).Value = u.Id;
                    worksheet.Cell(row, 2).Value = u.UserName;
                    worksheet.Cell(row, 3).Value = u.address;
                    worksheet.Cell(row, 4).Value = u.gender;
                    worksheet.Cell(row, 5).Value = u.age;
                    worksheet.Cell(row, 6).Value = u.Role;
                    worksheet.Cell(row, 7).Value = u.Email;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Customers.xlsx"
                    );
                }
            }
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

        public IActionResult Edituserfeedbacklogic(int id, string message)
        {
            var Feedback = bridge.feedbacks.Find(id);

            Feedback.message = message;

            bridge.SaveChanges();
            TempData["Message"] = " feedback  updated sucessfully";

            return RedirectToAction("Allusersfeedbacks");
        }


        public IActionResult Deleteuserfeebacklogic(int id)
        {


            var Feedbackid = bridge.feedbacks.Find(id);
            bridge.feedbacks.Remove(Feedbackid);
            bridge.SaveChanges();

            TempData["Message"] = "User feedback  deleted sucessfully";


            return RedirectToAction("Allusersfeedbacks");
        }

        public IActionResult Allproductsreviews()
        {
            var productreviews = bridge.productReviews
         .Include(p => p.Product)
         .Include(p => p.User)
         .ToList();

            return View(productreviews);

        }

       public IActionResult productreviewdetails(int id)
        {
            var productreview = bridge.productReviews.Find(id);

            //productreview.Include()


            return View();

        }



        public IActionResult Editproductreview(int id)
        {
            var productreview = bridge.productReviews.Find(id);
            return View(productreview);
        }

        public IActionResult Editproductreviewlogic(int id, string ReviewMessage, float Ratings)
        {
            var productreview = bridge.productReviews.Find(id);

            productreview.ReviewMessage=ReviewMessage; 
            productreview.Ratings=Ratings;
            bridge.SaveChanges();
            TempData["Message"] = "Product review updated sucessfully";

            return RedirectToAction("Allproductsreviews");
        }

        public IActionResult DeleteproductreviewLogic(int id)
        {
            var productreview = bridge.productReviews.Find(id);

            bridge.productReviews.Remove(productreview);
            bridge.SaveChanges();
            TempData["Message"] = "Product review deleted sucessfully";

            return View(productreview);
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

        public async Task<IActionResult> markorderasprocessinglogic(int id)
        {
            var order = bridge.orders.Find(id);

            if (order == null)
                return NotFound();

            var user = bridge.Users.Find(order.UserId);

            order.Status = "Processing";

            await bridge.SaveChangesAsync();

            var body = System.IO.File.ReadAllText("Views/Emails/OrderProcessing.html");

            body = body.Replace("{{Username}}", user.UserName);
            body = body.Replace("{{OrderNumber}}", order.Id.ToString());
            body = body.Replace("{{WebsiteUrl}}", "https://localhost:7015");

            await _emailSender.SendEmailAsync(
                user.Email,
                "🛠️ Your Order Is Being Processed",
                body);

            TempData["Message"] = $"Order marked as {order.Status} successfully.";

            return RedirectToAction("Myproductsorderdetails", new { id });
        }

        public async Task<IActionResult> markorderasdispatchedlogic(int id)
        {
            var order = bridge.orders.Find(id);

            if (order == null)
                return NotFound();

            var user = bridge.Users.Find(order.UserId);

            order.Status = "Dispatched";

            await bridge.SaveChangesAsync();

            var body = System.IO.File.ReadAllText("Views/Emails/OrderShipped.html");

            body = body.Replace("{{Username}}", user.UserName);
            body = body.Replace("{{OrderNumber}}", order.Id.ToString());
            body = body.Replace("{{TrackingNumber}}", "Not Available");
            body = body.Replace("{{EstimatedDelivery}}", DateTime.Now.AddDays(3).ToString("dd MMM yyyy"));
            body = body.Replace("{{WebsiteUrl}}", "https://localhost:7015");

            await _emailSender.SendEmailAsync(
                user.Email,
                "🚚 Your ArtGallery Order Has Been Dispatched",
                body);

            TempData["Message"] = $"Order marked as {order.Status} successfully.";

            return RedirectToAction("Myproductsorderdetails", new { id });
        }

        public async Task<IActionResult> markorderasdeliveredlogic(int id)
        {
            var order = bridge.orders.Find(id);

            if (order == null)
                return NotFound();

            var user = bridge.Users.Find(order.UserId);

            order.Status = "Delivered";

            await bridge.SaveChangesAsync();

            var body = System.IO.File.ReadAllText("Views/Emails/OrderDelivered.html");

            body = body.Replace("{{Username}}", user.UserName);
            body = body.Replace("{{OrderNumber}}", order.Id.ToString());
            body = body.Replace("{{WebsiteUrl}}", "https://localhost:7015");

            await _emailSender.SendEmailAsync(
                user.Email,
                "🎉 Your ArtGallery Order Has Been Delivered",
                body);

            TempData["Message"] = $"Order marked as {order.Status} successfully.";

            return RedirectToAction("Myproductsorderdetails", new { id });
        }

        public async Task<IActionResult> markorderasrejectedlogic(int id)
        {
            var order = bridge.orders.Find(id);

            if (order == null)
                return NotFound();

            var user = bridge.Users.Find(order.UserId);

            order.Status = "Rejected";

            await bridge.SaveChangesAsync();

            var body = System.IO.File.ReadAllText("Views/Emails/OrderRejected.html");

            body = body.Replace("{{Username}}", user.UserName);
            body = body.Replace("{{OrderNumber}}", order.Id.ToString());
            body = body.Replace("{{WebsiteUrl}}", "https://localhost:7015");

            await _emailSender.SendEmailAsync(
                user.Email,
                "❌ Your ArtGallery Order Has Been Rejected",
                body);

            TempData["Message"] = $"Order marked as {order.Status} successfully.";

            return RedirectToAction("Myproductsorderdetails", new { id });
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

            ViewBag.User = user;

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



        public IActionResult Myallproductspayments()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myPayments = bridge.payments
            .Include(p => p.Order)
            .Where(p => p.Order.UserId == userId)
            .ToList();

            return View(myPayments);
        }

        // my product payment delete logic
        public IActionResult Myallproductspaymentsdeletelogic(int id)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var payment = bridge.payments
                .Include(p => p.Order)
                .FirstOrDefault(p => p.Id == id && p.Order.UserId == userId);

            if (payment == null)
            {
                TempData["Message"] = "Payment not found or you do not have permission to delete it.";
                return RedirectToAction("Myallproductspayments");
            }

            bridge.payments.Remove(payment);
            bridge.SaveChanges();

            TempData["Message"] = "Payment deleted successfully.";
            return RedirectToAction("Myallproductspayments");
        }

        // - Rejected Orders page .
        public IActionResult Allrejectedproducts()
        {
            var rejectedproducts = bridge.products
             .Where(p => p.Status == "Rejected")
             .Include(p => p.SubCategory)
             .Include(p => p.User)
             .ToList();
            return View(rejectedproducts);
        }
        public IActionResult Allrejectedproductsdeletelogic(int id)
        {
            var product = bridge.products.FirstOrDefault(p => p.Id == id && p.Status == "Rejected");

            if (product == null)
            {
                TempData["Message"] = "Product not found or is not in Rejected status.";
                return RedirectToAction("AllrejcetedProducts");
            }

            bridge.products.Remove(product);
            bridge.SaveChanges();

            TempData["Message"] = "Rejected product deleted successfully.";
            return RedirectToAction("AllrejcetedProducts");
        }
        // get all orders details
        public IActionResult Allrejectedorders()
        {
            var rejectedOrders = bridge.orders
                .Where(o => o.Status == "Rejected")
                .Include(o => o.User)
                .Include(o => o.Product)
                .Include(o => o.Wishlist)
                .Include(o => o.Payment)
                .ToList();

            return View(rejectedOrders);
        }
        // Delete logic for all orders
        public IActionResult Allrejectedordersdeletelogic(int id)
        {
            var order = bridge.orders.FirstOrDefault(o => o.Id == id && o.Status == "Rejected");

            if (order == null)
            {
                TempData["Message"] = "Order not found or is not in Rejected status.";
                return RedirectToAction("AllRejectedOrders");
            }

            bridge.orders.Remove(order);
            bridge.SaveChanges();

            TempData["Message"] = "Rejected order deleted successfully.";
            return RedirectToAction("Allrejectedorders");
        }

        // admin all products page                                          
 

        // admin my products page
 

        // admin view my products added

     
        // create auction 
        public IActionResult Myauctionsproducts()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var auctionProducts = bridge.auctionDetails
                .Include(a => a.User)
                .Include(a => a.Product)
                    .ThenInclude(p => p.SubCategory)
                .Include(a => a.Product)
                    .ThenInclude(p => p.User)
                .Where(a => a.Product.AvailableForBid == "Yes" && a.Product.UserId == currentUserId)
                .ToList();

            return View(auctionProducts);
        }
        public IActionResult Myauctionsproductsdeletelogic(int id)
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var auctionproduct = bridge.auctionDetails
                .Include(a => a.Product)
                .FirstOrDefault(a => a.Id == id &&
                                     a.Product.UserId == UserId);

            if (auctionproduct == null)
            {
                return NotFound();
            }

            bridge.auctionDetails.Remove(auctionproduct);
            bridge.SaveChanges();

            TempData["Message"] = "Auction product deleted successfully.";

            return RedirectToAction("Myauctionsproducts");
        }

        public IActionResult Mybuynowproducts()
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var buynowproducts = bridge.products
                 .Include(p => p.User)
                 .Include(p => p.SubCategory)
                 .Where(p => (p.AvailableForBid == "No" || string.IsNullOrEmpty(p.AvailableForBid))
                             && p.UserId == UserId)
                 .ToList();

            return View(buynowproducts);
        }


        public IActionResult Mybuynowproductsdeletelogic(int id)
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = bridge.products
                .FirstOrDefault(p => p.Id == id &&
                                     p.UserId == UserId &&
                                     (p.AvailableForBid == "No" || string.IsNullOrEmpty(p.AvailableForBid)));

            if (product == null)
            {
                return NotFound();
            }

            bridge.products.Remove(product);
            bridge.SaveChanges();

            TempData["Message"] = "Product deleted successfully.";

            return RedirectToAction("Mybuynowproducts");
        }



        [HttpGet]
        public IActionResult GlobalSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            {
                return Json(new { products = Array.Empty<object>(), customers = Array.Empty<object>(), orders = Array.Empty<object>(), categories = Array.Empty<object>() });
            }

            term = term.Trim();
            bool isNumeric = int.TryParse(term, out int numericTerm);

            var products = bridge.products
                .Where(p => p.Name.Contains(term))
                .OrderBy(p => p.Name)
                .Take(5)
                .Select(p => new
                {
                    id = p.Id,
                    label = p.Name,
                    sub = p.Status,
                    url = Url.Action("Viewproductdetails", "Admin", new { id = p.Id })
                })
                .ToList();

            var customers = bridge.Users
                .Where(u => u.UserName.Contains(term) || (u.Email != null && u.Email.Contains(term)))
                .OrderBy(u => u.UserName)
                .Take(5)
                .Select(u => new
                {
                    id = u.Id,
                    label = u.UserName,
                    sub = u.Email,
                    url = Url.Action("Customerdetails", "Admin", new { id = u.Id })
                })
                .ToList();

            var ordersQuery = isNumeric
                ? bridge.orders.Where(o => o.Id == numericTerm || (o.ShippingAddress != null && o.ShippingAddress.Contains(term)))
                : bridge.orders.Where(o => o.ShippingAddress != null && o.ShippingAddress.Contains(term));

            var orders = ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    id = o.Id,
                    label = "Order #" + o.Id,
                    sub = o.Status,
                    url = Url.Action("Adminorderdetails", "Admin", new { id = o.Id })
                })
                .ToList();

            var categories = bridge.categories
                .Where(c => c.Name.Contains(term))
                .OrderBy(c => c.Name)
                .Take(5)
                .Select(c => new
                {
                    id = c.Id,
                    label = c.Name,
                    sub = "Category",
                    url = Url.Action("Allcategories", "Admin")
                })
                .ToList();

            return Json(new { products, customers, orders, categories });
        }









    }






}   