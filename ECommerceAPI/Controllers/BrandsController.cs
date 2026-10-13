using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController(ECommerceDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var brands = await context.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return Ok(brands);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var brand = await context.Brands
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

            if (brand == null)
                return NotFound(new { message = "Brand not found." });

            return Ok(brand);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Brand brand)
        {
            context.Brands.Add(brand);
            await context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = brand.Id },
                brand);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Brand brand)
        {
            var existing = await context.Brands.FindAsync(id);

            if (existing == null)
                return NotFound(new { message = "Brand not found." });

            existing.Name = brand.Name;
            existing.Description = brand.Description;
            existing.IsActive = brand.IsActive;

            await context.SaveChangesAsync();

            return Ok(existing);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await context.Brands.FindAsync(id);

            if (brand == null)
                return NotFound(new { message = "Brand not found." });

            brand.IsActive = false;
            await context.SaveChangesAsync();

            return Ok(new { message = "Brand deactivated successfully." });
        }
    }
}