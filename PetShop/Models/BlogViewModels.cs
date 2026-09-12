using PetShop.DataContext.Entites;

namespace PetShop.Models
{
    public class BlogListViewModel
    {
        public List<BlogPost> Posts { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public int PageSize { get; set; } = 6;
        public List<BlogPost> RecentPosts { get; set; } = new();
    }

    public class BlogDetailsViewModel
    {
        public BlogPost Post { get; set; } = null!;
        public List<BlogPost> RecentPosts { get; set; } = new();
    }
}
