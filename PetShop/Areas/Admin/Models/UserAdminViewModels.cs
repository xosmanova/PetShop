namespace PetShop.Areas.Admin.Models
{
    public class AdminUserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Initials
        {
            get
            {
                var f = !string.IsNullOrWhiteSpace(FirstName) ? FirstName[0].ToString().ToUpper() : "";
                var l = !string.IsNullOrWhiteSpace(LastName) ? LastName[0].ToString().ToUpper() : "";
                var res = $"{f}{l}";
                return string.IsNullOrEmpty(res) ? "US" : res;
            }
        }
        public string Role { get; set; } = "Viewer";
        public string Team { get; set; } = "Operations";
        public string Status { get; set; } = "Active"; // "Active", "Pending", "Suspended"
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    }

    public class UserListAdminViewModel
    {
        public List<AdminUserItemViewModel> Users { get; set; } = new();
        public string? Search { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 5));
        public int StartIndex => TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
        public int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);
    }

    public class CreateUserAdminViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Editor";
        public string Team { get; set; } = "Operations";
        public string Status { get; set; } = "Active";
        public string Password { get; set; } = string.Empty;
    }

    public class UserActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string DotColor { get; set; } = "primary"; // primary (blue), success (green), warning (orange)
    }

    public class AdminUserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Position { get; set; } = "Senior Administrator";
        public string Role { get; set; } = "Admin";
        public string Team { get; set; } = "Management";
        public string Status { get; set; } = "Active"; // Active, Pending, Suspended
        public string Location { get; set; } = "Baku, Azerbaijan";
        public string LastLogin { get; set; } = "2 hours ago";
        public string Projects { get; set; } = "12 Active";
        public string? AvatarUrl { get; set; }
        public string Initials
        {
            get
            {
                var f = !string.IsNullOrWhiteSpace(FirstName) ? FirstName[0].ToString().ToUpper() : "";
                var l = !string.IsNullOrWhiteSpace(LastName) ? LastName[0].ToString().ToUpper() : "";
                var res = $"{f}{l}";
                return string.IsNullOrEmpty(res) ? "US" : res;
            }
        }
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
        public List<UserActivityItem> Activities { get; set; } = new();
    }
}
