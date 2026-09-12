namespace PetShop.DataContext.Entites
{
    public class Basket
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = null!;
        public string? UserId { get; set; } // Nullable - Login olanda UserId-yə bağlanır
        public ApplicationUser? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public List<BasketItem> BasketItems { get; set; } = new();
    }
}
