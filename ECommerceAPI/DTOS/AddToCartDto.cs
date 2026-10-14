
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs
{
    public class AddToCartDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int ProductVariantId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
