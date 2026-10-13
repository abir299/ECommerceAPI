namespace ECommerceAPI.Data.Entities
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public decimal DiscountPercentage { get; set; }

        public int CategoryId { get; set; }

        public int BrandId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Category Category { get; set; } = null!;

        public Brand Brand { get; set; } = null!;

        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

        public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public Stock? Stock { get; set; }
    }
}