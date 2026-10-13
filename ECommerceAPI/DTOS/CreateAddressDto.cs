namespace ECommerceAPI.DTOs
{
    public class CreateAddressDto
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string City { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public bool IsDefault { get; set; }
    }
}