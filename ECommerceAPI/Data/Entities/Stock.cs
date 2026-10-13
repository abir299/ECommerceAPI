namespace ECommerceAPI.Data.Entities
{
    public class Stock
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public int ReservedQuantity { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Product Product { get; set; } = null!;
    }
}