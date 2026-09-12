using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Areas.Admin.Models;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;

            // 1. Ümumi sifariş sayı
            var totalOrders = await _context.Orders.CountAsync();

            // 2. Bugünkü satış məbləği (Ləğv edilmişlər çıxılmaqla)
            var todaySales = await _context.Orders
                .Where(o => o.CreatedDate.Date == today && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            // 3. Ümumi gəlir
            var totalRevenue = await _context.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            // 4. Aktiv məhsul sayı
            var activeProductsCount = await _context.Products.CountAsync(p => p.IsActive);

            // 5. Son 5 sifariş siyahısı
            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedDate)
                .Take(5)
                .ToListAsync();

            // 6. Bu il üzrə aylıq satış məbləği (Status == OrderStatus.Paid)
            var currentYear = DateTime.Now.Year;
            var monthlySalesGrouped = await _context.Orders
                .Where(o => o.CreatedDate.Year == currentYear && o.Status == OrderStatus.Paid)
                .GroupBy(o => o.CreatedDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalPrice) })
                .ToListAsync();

            var monthlySales = new decimal[12];
            foreach (var item in monthlySalesGrouped)
            {
                if (item.Month >= 1 && item.Month <= 12)
                {
                    monthlySales[item.Month - 1] = item.Total;
                }
            }

            ViewBag.MonthlySales = monthlySales;

            var viewModel = new DashboardViewModel
            {
                TotalOrders = totalOrders,
                TodaySales = todaySales,
                TotalRevenue = totalRevenue,
                ActiveProductsCount = activeProductsCount,
                RecentOrders = recentOrders,
                MonthlySales = monthlySales
            };

            return View(viewModel);
        }

        // GET: /Admin/Dashboard/GlobalSearch?query=...
        [HttpGet]
        public async Task<IActionResult> GlobalSearch(string? query)
        {
            var searchTerm = (query ?? Request.Query["q"].ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return Json(new List<object>());
            }

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Name.Contains(searchTerm) || (p.Sku != null && p.Sku.Contains(searchTerm)))
                .Take(5)
                .Select(p => new
                {
                    type = "Product",
                    id = p.Id.ToString(),
                    name = p.Name,
                    detail = (p.DiscountPrice.HasValue ? p.DiscountPrice.Value : p.Price).ToString("0.00") + " ₼" + (p.Sku != null ? " • SKU: " + p.Sku : ""),
                    url = $"/Admin/Products/Edit/{p.Id}"
                })
                .ToListAsync();

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderNumber.Contains(searchTerm) || o.CustomerName.Contains(searchTerm) || o.Email.Contains(searchTerm))
                .Take(5)
                .Select(o => new
                {
                    type = "Order",
                    id = o.Id.ToString(),
                    name = o.OrderNumber,
                    detail = o.CustomerName + " • " + o.TotalPrice.ToString("0.00") + " ₼",
                    url = $"/Admin/Orders/Details/{o.Id}"
                })
                .ToListAsync();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => (u.Email != null && u.Email.Contains(searchTerm)) || 
                            (u.FirstName != null && u.FirstName.Contains(searchTerm)) || 
                            (u.LastName != null && u.LastName.Contains(searchTerm)) ||
                            (u.UserName != null && u.UserName.Contains(searchTerm)))
                .Take(5)
                .Select(u => new
                {
                    type = "User",
                    id = u.Id,
                    name = (!string.IsNullOrWhiteSpace(u.FirstName) ? (u.FirstName + " " + (u.LastName ?? "")).Trim() : (u.Email ?? u.UserName ?? "User")),
                    detail = u.Email ?? u.UserName ?? "",
                    url = $"/Admin/Users/Details/{u.Id}"
                })
                .ToListAsync();

            var results = products.Concat<object>(orders).Concat(users).ToList();
            return Json(results);
        }
    }
}
