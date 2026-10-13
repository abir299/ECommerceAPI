using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using ECommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(ECommerceDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(
            string? search,
            int? categoryId,
            int? brandId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy = "name",
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1 || pageSize > 50)
                pageSize = 10;

            var query = context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null &&
                     p.Description.Contains(search)));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sortBy?.ToLower() switch
            {
                "priceasc" => query.OrderBy(p => p.Price),
                "pricedesc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };

            var totalItems = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    DiscountPercentage = p.DiscountPercentage,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    BrandId = p.BrandId,
                    BrandName = p.Brand.Name,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(
                    totalItems / (double)pageSize),
                data = products
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    DiscountPercentage = p.DiscountPercentage,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    BrandId = p.BrandId,
                    BrandName = p.Brand.Name,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound(new { message = "Product not found." });

            return Ok(product);
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            if (dto.Price < 0)
                return BadRequest(new { message = "Price cannot be negative." });

            if (dto.DiscountPercentage < 0 ||
                dto.DiscountPercentage > 100)
                return BadRequest(new
                {
                    message = "Discount must be between 0 and 100."
                });

            var categoryExists = await context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.IsActive);

            if (!categoryExists)
                return BadRequest(new { message = "Invalid category." });

            var brandExists = await context.Brands
                .AnyAsync(b => b.Id == dto.BrandId && b.IsActive);

            if (!brandExists)
                return BadRequest(new { message = "Invalid brand." });

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                DiscountPercentage = dto.DiscountPercentage,
                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                product);
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            CreateProductDto dto)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (dto.Price < 0)
                return BadRequest(new { message = "Price cannot be negative." });

            if (dto.DiscountPercentage < 0 ||
                dto.DiscountPercentage > 100)
                return BadRequest(new
                {
                    message = "Discount must be between 0 and 100."
                });

            var categoryExists = await context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId && c.IsActive);

            if (!categoryExists)
                return BadRequest(new { message = "Invalid category." });

            var brandExists = await context.Brands
                .AnyAsync(b => b.Id == dto.BrandId && b.IsActive);

            if (!brandExists)
                return BadRequest(new { message = "Invalid brand." });

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.DiscountPercentage = dto.DiscountPercentage;
            product.CategoryId = dto.CategoryId;
            product.BrandId = dto.BrandId;

            await context.SaveChangesAsync();

            return Ok(product);
        }

        [Authorize(Roles = "Admin,Seller")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            product.IsActive = false;

            await context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product deactivated successfully."
            });
        }
    }
}