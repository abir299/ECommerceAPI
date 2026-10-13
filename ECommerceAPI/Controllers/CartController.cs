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
        private int UserId => int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var cart = await context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == UserId);

            if (cart == null)
                return Ok(new { items = new object[0], total = 0m });

            var items = cart.CartItems.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product.Name,
                i.Quantity,
                i.Product.Price,
                Subtotal = i.Product.Price * i.Quantity
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
            var product = await context.Products.FirstOrDefaultAsync(
                p => p.Id == dto.ProductId && p.IsActive);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            var cart = await context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == UserId);

            if (cart == null)
            {
                cart = new Cart { UserId = UserId };
                context.Carts.Add(cart);
                await context.SaveChangesAsync();
            }

            var item = cart.CartItems.FirstOrDefault(
                i => i.ProductId == dto.ProductId);

            if (item == null)
            {
                context.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }
            else
            {
                item.Quantity += dto.Quantity;
            }

            await context.SaveChangesAsync();

            return Ok(new { message = "Product added to cart." });
        }

        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateQuantity(
            int productId, [FromBody] int quantity)
        {
            if (quantity < 1 || quantity > 100)
                return BadRequest(new { message = "Quantity must be 1-100." });

            var item = await context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i =>
                    i.ProductId == productId && i.Cart.UserId == UserId);

            if (item == null)
                return NotFound(new { message = "Cart item not found." });

            item.Quantity = quantity;
            await context.SaveChangesAsync();

            return Ok(new { message = "Cart quantity updated." });
        }

        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var item = await context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i =>
                    i.ProductId == productId && i.Cart.UserId == UserId);

            if (item == null)
                return NotFound(new { message = "Cart item not found." });

            context.CartItems.Remove(item);
            await context.SaveChangesAsync();

            return Ok(new { message = "Product removed from cart." });
        }
    }
}