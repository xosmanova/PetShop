using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.DataContext;
using PetShop.Models;

namespace PetShop.Controllers
{
    public class BlogController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 6;

        public BlogController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Blog or /Blog/Index?page=1
        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1) page = 1;

            var query = _context.BlogPosts
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedDate);

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            if (page > totalPages) page = totalPages;

            var posts = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var recentPosts = await _context.BlogPosts
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedDate)
                .Take(4)
                .ToListAsync();

            var model = new BlogListViewModel
            {
                Posts = posts,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                RecentPosts = recentPosts
            };

            return View(model);
        }

        // GET: /blog/{slug} or /Blog/Details?slug=...
        public async Task<IActionResult> Details(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.Equals(slug, "Details", StringComparison.OrdinalIgnoreCase))
            {
                slug = Request.Query["slug"].ToString();
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var post = await _context.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Slug == slug);

            if (post == null)
            {
                return NotFound();
            }

            var recentPosts = await _context.BlogPosts
                .AsNoTracking()
                .Where(b => b.Id != post.Id)
                .OrderByDescending(b => b.CreatedDate)
                .Take(3)
                .ToListAsync();

            var model = new BlogDetailsViewModel
            {
                Post = post,
                RecentPosts = recentPosts
            };

            return View(model);
        }
    }
}
