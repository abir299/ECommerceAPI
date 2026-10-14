
namespace ECommerceAPI.Data.Entities
{
    public class Address
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; } = null!;

        public string Phone { get; set; } = null!;

        public string AddressLine { get; set; } = null!;

        public string City { get; set; } = null!;

        public string PostalCode { get; set; } = null!;

        public bool IsDefault { get; set; }

        public User User { get; set; } = null!;
    }
}
