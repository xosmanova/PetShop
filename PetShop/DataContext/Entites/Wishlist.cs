namespace PetShop.DataContext.Entites
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public DateTime AddedDate { get; set; } = DateTime.Now;
    }
}
