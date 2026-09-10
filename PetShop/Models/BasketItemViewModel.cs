namespace PetShop.Models
{
    public class BasketItemViewModel
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }
}
