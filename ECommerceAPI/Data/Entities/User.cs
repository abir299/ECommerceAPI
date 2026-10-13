namespace ECommerceAPI.Data.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string Role { get; set; } = "Customer";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

        public ICollection<Address> Addresses { get; set; } = new List<Address>();

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}