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
    public class ReviewsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;

        public ReviewsController(ECommerceDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

            return int.TryParse(userId, out var id) ? id : 0;
        }

        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
                return NotFound("Product not found.");

            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.ProductId,
                    r.UserId,
                    UserName = r.User.FullName,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("{productId:int}")]
        public async Task<IActionResult> AddReview(
            int productId, [FromBody] ReviewRequest request)
        {
            var userId = GetUserId();

            if (userId <= 0)
                return Unauthorized();

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var productExists = await _context.Products
                .AnyAsync(p => p.Id == productId);

            if (!productExists)
                return NotFound("Product not found.");

            var alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == userId);

            if (alreadyReviewed)
                return BadRequest("You have already reviewed this product.");

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Review added successfully.",
                reviewId = review.Id
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userId = GetUserId();

            if (userId <= 0)
                return Unauthorized();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null)
                return NotFound("Review not found.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review deleted successfully." });
        }
    }

    public class ReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}