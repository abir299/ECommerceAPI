using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs
{
    public class CreateOrderDto
    {
        [Range(1, int.MaxValue)]
        public int ShippingAddressId { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "CashOnDelivery";
    }
}