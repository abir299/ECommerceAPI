namespace ECommerceAPI.Data.Entities
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string Size { get; set; } = null!;

        public string Color { get; set; } = null!;

        public decimal AdditionalPrice { get; set; }

        public int StockQuantity { get; set; }

        public Product Product { get; set; } = null!;
    }
}