using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using ECommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressesController(ECommerceDbContext context) : ControllerBase
    {
        private int UserId => int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var addresses = await context.Addresses
                .Where(a => a.UserId == UserId)
                .ToListAsync();

            return Ok(addresses);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAddressDto dto)
        {
            var address = new Address
            {
                UserId = UserId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                AddressLine = dto.AddressLine,
                City = dto.City,
                PostalCode = dto.PostalCode,
                IsDefault = dto.IsDefault
            };

            context.Addresses.Add(address);
            await context.SaveChangesAsync();

            return Ok(new { address.Id, message = "Address created." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var address = await context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);

            if (address == null)
                return NotFound();

            context.Addresses.Remove(address);
            await context.SaveChangesAsync();

            return Ok(new { message = "Address deleted." });
        }
    }
}