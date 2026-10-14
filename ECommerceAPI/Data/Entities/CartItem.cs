
namespace ECommerceAPI.Data.Entities
{
    public class CartItem
    {
        public int Id { get; set; }

        public int CartId { get; set; }

        public int ProductId { get; set; }

        public int? ProductVariantId { get; set; }

        public int Quantity { get; set; }

        public Cart Cart { get; set; } = null!;

        public Product Product { get; set; } = null!;

        public ProductVariant? ProductVariant { get; set; }
    }
}
