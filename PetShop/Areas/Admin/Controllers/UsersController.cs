using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Areas.Admin.Models;
using PetShop.DataContext;
using PetShop.DataContext.Entites;

namespace PetShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 5;

            var detailedUsers = await GetAllDetailedUsersAsync();

            var userList = detailedUsers.Select(d => new AdminUserItemViewModel
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                Role = d.Role,
                Team = d.Team,
                Status = d.Status,
                JoinedDate = d.JoinedDate
            }).AsEnumerable();

            // Axtarış filtri
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                userList = userList.Where(u =>
                    u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Team.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Status.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var filteredList = userList.ToList();
            var totalCount = filteredList.Count;
            var pagedItems = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new UserListAdminViewModel
            {
                Users = pagedItems,
                Search = search,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(viewModel);
        }

        // GET: /Admin/Users/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var allUsers = await GetAllDetailedUsersAsync();
            var user = allUsers.FirstOrDefault(u =>
                u.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: /Admin/Users/Edit/{id}
        [HttpGet]
        public IActionResult Edit(string id)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Admin/Users/AddUser
        [HttpGet]
        public IActionResult AddUser()
        {
            return View(new CreateUserAdminViewModel());
        }

        // POST: /Admin/Users/AddUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(CreateUserAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Bu e-poçt ünvanı artıq mövcuddur.");
                return View(model);
            }

            var newUser = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Department = model.Team.Trim(),
                JobTitle = model.Role.Trim(),
                EmailConfirmed = true
            };

            var pwd = !string.IsNullOrWhiteSpace(model.Password) ? model.Password : "User123!";
            var result = await _userManager.CreateAsync(newUser, pwd);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            TempData["Success"] = "Yeni istifadəçi uğurla yaradıldı.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<AdminUserDetailsViewModel>> GetAllDetailedUsersAsync()
        {
            var dbUsers = await _context.Users.AsNoTracking().ToListAsync();
            var userList = new List<AdminUserDetailsViewModel>();

            // Layihə kontekstinə uyğun PetShop komanda profilləri
            var predefinedProfiles = new Dictionary<string, (string Position, string Role, string Team, string Status, string Location, string LastLogin, string Projects, string DefaultPhone, List<UserActivityItem> Activities)>(StringComparer.OrdinalIgnoreCase)
            {
                ["admin@petshop.com"] = (
                    "Senior Administrator", "Admin", "Management", "Active", "Baku, Azerbaijan", "2 hours ago", "12 Active", "+994 50 123 45 67",
                    new List<UserActivityItem>
                    {
                        new() { Title = "Updated billing permissions", TimeAgo = "2 hours ago", DotColor = "primary" },
                        new() { Title = "Approved new teammate", TimeAgo = "Yesterday", DotColor = "success" },
                        new() { Title = "Changed password", TimeAgo = "Apr 30, 2026", DotColor = "warning" }
                    }),
                ["aygun@gmail.com"] = (
                    "Store Operations Lead", "Store Manager", "Operations", "Active", "Baku, Azerbaijan", "45 minutes ago", "8 Active", "+994 50 234 56 78",
                    new List<UserActivityItem>
                    {
                        new() { Title = "Updated inventory catalog items", TimeAgo = "45 minutes ago", DotColor = "primary" },
                        new() { Title = "Processed batch orders for fulfillment", TimeAgo = "Yesterday", DotColor = "success" },
                        new() { Title = "Exported monthly sales report", TimeAgo = "May 02, 2026", DotColor = "info" }
                    }),
                ["nigar@gmail.com"] = (
                    "Head of Content & Growth", "Content Editor", "Marketing", "Active", "Sumgayit, Azerbaijan", "3 hours ago", "5 Active", "+994 55 345 67 89",
                    new List<UserActivityItem>
                    {
                        new() { Title = "Published new blog article 'Pet Nutrition Tips'", TimeAgo = "3 hours ago", DotColor = "success" },
                        new() { Title = "Scheduled summer promotional banners", TimeAgo = "2 days ago", DotColor = "primary" },
                        new() { Title = "Updated coupon discounts for Pet Foodies", TimeAgo = "May 01, 2026", DotColor = "warning" }
                    }),
                ["eli@gmail.com"] = (
                    "Customer Experience Specialist", "Customer Support", "Support", "Pending", "Ganja, Azerbaijan", "Pending activation", "1 Workspace", "+994 70 456 78 90",
                    new List<UserActivityItem>
                    {
                        new() { Title = "Account invitation sent to email", TimeAgo = "1 day ago", DotColor = "warning" },
                        new() { Title = "Assigned to Customer Care onboarding queue", TimeAgo = "2 days ago", DotColor = "primary" },
                        new() { Title = "Profile created by Administrator", TimeAgo = "May 04, 2026", DotColor = "info" }
                    }),
                ["ali@test.com"] = (
                    "Logistics Coordinator", "Viewer", "Logistics", "Suspended", "Baku, Azerbaijan", "14 days ago", "0 Active", "+994 51 567 89 01",
                    new List<UserActivityItem>
                    {
                        new() { Title = "Account suspended due to policy inactivity", TimeAgo = "14 days ago", DotColor = "warning" },
                        new() { Title = "Revoked access to shipping warehouse API", TimeAgo = "14 days ago", DotColor = "danger" },
                        new() { Title = "Last system login from desktop", TimeAgo = "Apr 18, 2026", DotColor = "primary" }
                    }),
                ["mehseti@gmail.com"] = (
                    "E-Commerce Product Lead", "Product Manager", "E-Commerce", "Active", "Baku, Azerbaijan", "1 hour ago", "6 Categories", "+994 50 678 90 12",
                    new List<UserActivityItem>
                    {
                        new() { Title = "Added 4 new variant SKUs to Whiskas products", TimeAgo = "1 hour ago", DotColor = "success" },
                        new() { Title = "Reviewed user feedback and product ratings", TimeAgo = "Yesterday", DotColor = "primary" },
                        new() { Title = "Updated brand metadata for Royal Canin", TimeAgo = "Apr 29, 2026", DotColor = "info" }
                    }),
                ["reviewer2330@example.com"] = (
                    "Senior QA & Product Reviewer", "Content Reviewer", "Product Review", "Active", "Baku, Azerbaijan", "5 hours ago", "3 Review Queues", "+994 55 789 01 23",
                    new List<UserActivityItem>
                    {
                        new() { Title = "Approved 15 pending customer reviews", TimeAgo = "5 hours ago", DotColor = "success" },
                        new() { Title = "Flagged spam rating submission", TimeAgo = "2 days ago", DotColor = "warning" },
                        new() { Title = "Generated weekly quality compliance digest", TimeAgo = "Apr 27, 2026", DotColor = "primary" }
                    })
            };

            foreach (var u in dbUsers)
            {
                var email = u.Email ?? u.UserName ?? string.Empty;
                string position = !string.IsNullOrWhiteSpace(u.JobTitle) ? u.JobTitle : "Team Specialist";
                string role = "Viewer";
                string team = !string.IsNullOrWhiteSpace(u.Department) ? u.Department : "Operations";
                string status = "Active";
                string location = "Baku, Azerbaijan";
                string lastLogin = "Recently";
                string projects = "2 Workspaces";
                string phone = !string.IsNullOrWhiteSpace(u.PhoneNumber) ? u.PhoneNumber : "+994 50 000 00 00";
                var activities = new List<UserActivityItem>
                {
                    new() { Title = "Signed in to workspace", TimeAgo = "Recently", DotColor = "primary" },
                    new() { Title = "Updated profile information", TimeAgo = "Earlier", DotColor = "success" },
                    new() { Title = "Password confirmed", TimeAgo = "Apr 2026", DotColor = "warning" }
                };

                if (predefinedProfiles.TryGetValue(email, out var profile))
                {
                    position = !string.IsNullOrWhiteSpace(u.JobTitle) ? u.JobTitle : profile.Position;
                    role = profile.Role;
                    team = !string.IsNullOrWhiteSpace(u.Department) ? u.Department : profile.Team;
                    status = profile.Status;
                    location = profile.Location;
                    lastLogin = profile.LastLogin;
                    projects = profile.Projects;
                    phone = !string.IsNullOrWhiteSpace(u.PhoneNumber) ? u.PhoneNumber : profile.DefaultPhone;
                    activities = profile.Activities;
                }

                userList.Add(new AdminUserDetailsViewModel
                {
                    Id = u.Id,
                    FirstName = !string.IsNullOrWhiteSpace(u.FirstName) ? u.FirstName : email.Split('@')[0],
                    LastName = !string.IsNullOrWhiteSpace(u.LastName) ? u.LastName : "",
                    Email = email,
                    PhoneNumber = phone,
                    Position = position,
                    Role = role,
                    Team = team,
                    Status = status,
                    Location = location,
                    LastLogin = lastLogin,
                    Projects = projects,
                    JoinedDate = DateTime.UtcNow.AddMonths(-6),
                    Activities = activities
                });
            }

            return userList;
        }
    }
}
