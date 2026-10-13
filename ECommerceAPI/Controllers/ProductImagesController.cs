using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductImagesController : ControllerBase
    {
        private readonly ECommerceDbContext _context;

        public ProductImagesController(ECommerceDbContext context)
        {
            _context = context;
        }

        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
                return NotFound("Product not found.");

            var images = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ImageUrl,
                    i.IsPrimary
                })
                .ToListAsync();

            return Ok(images);
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpPost("{productId:int}")]
        public async Task<IActionResult> AddProductImage(
            int productId, [FromBody] ProductImageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageUrl))
                return BadRequest("ImageUrl is required.");

            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
                return NotFound("Product not found.");

            if (request.IsPrimary)
            {
                var existingImages = await _context.ProductImages
                    .Where(i => i.ProductId == productId && i.IsPrimary)
                    .ToListAsync();

                foreach (var image in existingImages)
                    image.IsPrimary = false;
            }

            var productImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = request.ImageUrl.Trim(),
                IsPrimary = request.IsPrimary
            };

            _context.ProductImages.Add(productImage);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product image added successfully.",
                productImageId = productImage.Id
            });
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            var image = await _context.ProductImages
                .FindAsync(id);

            if (image == null)
                return NotFound("Product image not found.");

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product image deleted successfully."
            });
        }
    }

    public class ProductImageRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}