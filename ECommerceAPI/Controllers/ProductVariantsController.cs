using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductVariantsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;

        public ProductVariantsController(ECommerceDbContext context)
        {
            _context = context;
        }

        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
                return NotFound("Product not found.");

            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .Select(v => new
                {
                    v.Id,
                    v.ProductId,
                    v.Size,
                    v.Color,
                    v.AdditionalPrice,
                    v.StockQuantity
                })
                .ToListAsync();

            return Ok(variants);
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpPost("{productId:int}")]
        public async Task<IActionResult> AddVariant(
            int productId, [FromBody] ProductVariantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Size) ||
                string.IsNullOrWhiteSpace(request.Color))
            {
                return BadRequest("Size and Color are required.");
            }

            if (request.AdditionalPrice < 0 || request.StockQuantity < 0)
                return BadRequest("Price and stock cannot be negative.");

            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
                return NotFound("Product not found.");

            var variant = new ProductVariant
            {
                ProductId = productId,
                Size = request.Size.Trim(),
                Color = request.Color.Trim(),
                AdditionalPrice = request.AdditionalPrice,
                StockQuantity = request.StockQuantity
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product variant added successfully.",
                variantId = variant.Id
            });
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateVariant(
            int id, [FromBody] ProductVariantRequest request)
        {
            var variant = await _context.ProductVariants.FindAsync(id);

            if (variant == null)
                return NotFound("Product variant not found.");

            if (string.IsNullOrWhiteSpace(request.Size) ||
                string.IsNullOrWhiteSpace(request.Color))
            {
                return BadRequest("Size and Color are required.");
            }

            if (request.AdditionalPrice < 0 || request.StockQuantity < 0)
                return BadRequest("Price and stock cannot be negative.");

            variant.Size = request.Size.Trim();
            variant.Color = request.Color.Trim();
            variant.AdditionalPrice = request.AdditionalPrice;
            variant.StockQuantity = request.StockQuantity;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Product variant updated successfully." });
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);

            if (variant == null)
                return NotFound("Product variant not found.");

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product variant deleted successfully." });
        }
    }

    public class ProductVariantRequest
    {
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; }
        public int StockQuantity { get; set; }
    }
}