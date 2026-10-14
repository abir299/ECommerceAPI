
using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using ECommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CartController(ECommerceDbContext context) : ControllerBase
    {
        private int UserId =>
            int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub"),
                out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var cart = await context.Carts
                .AsNoTracking()
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync(c => c.UserId == UserId);

            if (cart == null)
                return Ok(new { items = new object[0], total = 0m });

            var items = cart.CartItems.Select(i =>
            {
                var variant = i.ProductVariant;
                var unitPrice = i.Product.Price +
                    (variant?.AdditionalPrice ?? 0m);

                return new
                {
                    i.ProductId,
                    ProductName = i.Product.Name,
                    i.ProductVariantId,
                    Size = variant?.Size,
                    Color = variant?.Color,
                    i.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = unitPrice * i.Quantity
                };
            }).ToList();

            return Ok(new
            {
                items,
                total = items.Sum(i => i.Subtotal)
            });
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart(AddToCartDto dto)
        {
            if (dto.Quantity < 1 || dto.Quantity > 100)
                return BadRequest(new
                {
                    message = "Quantity must be between 1 and 100."
                });

            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == dto.ProductId && p.IsActive);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            var variant = await context.ProductVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(v =>
                    v.Id == dto.ProductVariantId &&
                    v.ProductId == dto.ProductId);

            if (variant == null)
                return BadRequest(new
                {
                    message = "Invalid variant for this product."
                });

            if (variant.StockQuantity < dto.Quantity)
                return BadRequest(new
                {
                    message = "Insufficient variant stock."
                });

            var cart = await context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == UserId);

            if (cart == null)
            {
                cart = new Cart { UserId = UserId };
                context.Carts.Add(cart);
                await context.SaveChangesAsync();
            }

            var item = cart.CartItems.FirstOrDefault(i =>
                i.ProductId == dto.ProductId &&
                i.ProductVariantId == dto.ProductVariantId);

            var newQuantity = (item?.Quantity ?? 0) + dto.Quantity;

            if (newQuantity > 100)
                return BadRequest(new
                {
                    message = "Cart quantity cannot exceed 100."
                });

            if (newQuantity > variant.StockQuantity)
                return BadRequest(new
                {
                    message = "Requested quantity exceeds available stock."
                });

            if (item == null)
            {
                context.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    ProductVariantId = dto.ProductVariantId,
                    Quantity = dto.Quantity
                });
            }
            else
            {
                item.Quantity = newQuantity;
            }

            await context.SaveChangesAsync();

            return Ok(new { message = "Variant added to cart." });
        }

        [HttpPut("items/{variantId:int}")]
        public async Task<IActionResult> UpdateQuantity(
            int variantId,
            [FromBody] int quantity)
        {
            if (quantity < 1 || quantity > 100)
                return BadRequest(new
                {
                    message = "Quantity must be between 1 and 100."
                });

            var item = await context.CartItems
                .Include(i => i.Cart)
                .Include(i => i.ProductVariant)
                .FirstOrDefaultAsync(i =>
                    i.ProductVariantId == variantId &&
                    i.Cart.UserId == UserId);

            if (item == null)
                return NotFound(new { message = "Cart variant not found." });

            if (item.ProductVariant == null ||
                quantity > item.ProductVariant.StockQuantity)
                return BadRequest(new
                {
                    message = "Requested quantity exceeds available stock."
                });

            item.Quantity = quantity;
            await context.SaveChangesAsync();

            return Ok(new { message = "Cart quantity updated." });
        }

        [HttpDelete("items/{variantId:int}")]
        public async Task<IActionResult> RemoveItem(int variantId)
        {
            var item = await context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i =>
                    i.ProductVariantId == variantId &&
                    i.Cart.UserId == UserId);

            if (item == null)
                return NotFound(new { message = "Cart variant not found." });

            context.CartItems.Remove(item);
            await context.SaveChangesAsync();

            return Ok(new { message = "Variant removed from cart." });
        }
    }
}
