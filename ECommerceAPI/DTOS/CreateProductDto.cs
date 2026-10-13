namespace ECommerceAPI.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public decimal DiscountPercentage { get; set; }

        public int CategoryId { get; set; }

        public int BrandId { get; set; }
    }
}