namespace ECommerceAPI.Data.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ShippingAddressId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;

        public Address ShippingAddress { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public Payment? Payment { get; set; }
    }
}