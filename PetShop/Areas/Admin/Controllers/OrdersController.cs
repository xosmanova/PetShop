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
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(
            OrderStatus? status, 
            DateTime? startDate, 
            DateTime? endDate, 
            string? search, 
            int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .AsQueryable();

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            // Tarix filteri
            if (startDate.HasValue)
            {
                query = query.Where(o => o.CreatedDate >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedDate <= endOfDay);
            }

            // Axtarış (Sifariş nömrəsi, müştəri adı, email, telefon)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderNumber.ToLower().Contains(lower) ||
                    o.CustomerName.ToLower().Contains(lower) ||
                    o.Email.ToLower().Contains(lower) ||
                    o.Phone.ToLower().Contains(lower));
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var orders = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new OrderListAdminViewModel
            {
                Orders = orders,
                SelectedStatus = status,
                StartDate = startDate,
                EndDate = endDate,
                Search = search,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Sifariş tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        // POST: Admin/Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Sifariş tapılmadı!";
                return RedirectToAction(nameof(Index));
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"#{order.OrderNumber} nömrəli sifarişin statusu '{newStatus}' olaraq uğurla yeniləndi!";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}
