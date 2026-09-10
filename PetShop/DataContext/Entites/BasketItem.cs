namespace PetShop.DataContext.Entites
{
    public class BasketItem
    {
        public int Id { get; set; }

        public int BasketId { get; set; }
        public Basket Basket { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
