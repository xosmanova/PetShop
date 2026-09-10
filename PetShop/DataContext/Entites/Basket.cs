using Microsoft.AspNetCore.Identity;

namespace PetShop.DataContext.Entites
{
    public class Basket
    {

        public int Id { get; set; }
        public string SessionId { get; set; } = null!;   // ⚠️ İndi UserId yerinə, sonra Login yazanda dəyişəcəyik
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public List<BasketItem> BasketItems { get; set; } = new();





    }
}
