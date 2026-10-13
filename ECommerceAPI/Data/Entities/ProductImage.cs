namespace ECommerceAPI.Data.Entities
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ImageUrl { get; set; } = null!;

        public bool IsPrimary { get; set; }

        public Product Product { get; set; } = null!;
    }
}