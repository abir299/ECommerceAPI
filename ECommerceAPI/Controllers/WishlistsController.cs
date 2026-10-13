
using System.Security.Claims;
using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class WishlistsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;

        public WishlistsController(ECommerceDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

            return int.TryParse(userId, out var id) ? id : 0;
        }

        private async Task<Wishlist> GetOrCreateWishlist(int userId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist != null)
                return wishlist;

            wishlist = new Wishlist { UserId = userId };

            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();

            return wishlist;
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = GetUserId();

            if (userId <= 0)
                return Unauthorized();

            var wishlist = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => new
                {
                    w.Id,
                    w.CreatedAt,
                    Items = w.WishlistItems.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        ProductName = i.Product.Name,
                        Price = i.Product.Price
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (wishlist == null)
            {
                return Ok(new
                {
                    id = (int?)null,
                    items = new object[0]
                });
            }

            return Ok(wishlist);
        }

        [HttpPost("items/{productId:int}")]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            var userId = GetUserId();

            if (userId <= 0)
                return Unauthorized();

            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
                return NotFound("Product not found.");

            var wishlist = await GetOrCreateWishlist(userId);

            var alreadyExists = await _context.WishlistItems
                .AnyAsync(i =>
                    i.WishlistId == wishlist.Id &&
                    i.ProductId == productId);

            if (alreadyExists)
                return BadRequest("Product is already in your wishlist.");

            var item = new WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = productId
            };

            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product added to wishlist.",
                wishlistItemId = item.Id,
                productId
            });
        }

        [HttpDelete("items/{productId:int}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var userId = GetUserId();

            if (userId <= 0)
                return Unauthorized();

            var wishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist == null)
                return NotFound("Wishlist not found.");

            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(i =>
                    i.WishlistId == wishlist.Id &&
                    i.ProductId == productId);

            if (item == null)
                return NotFound("Product not found in wishlist.");

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product removed from wishlist.",
                productId
            });
        }
    }
}
