using ECommerceAPI.Data;
using ECommerceAPI.Data.Entities;
using ECommerceAPI.DTOs;
using ECommerceAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ECommerceDbContext _context;

        public AuthService(ECommerceDbContext context)
        {
            _context = context;
        }

        public async Task<string> Register(RegisterDto dto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (existingUser != null)
                return "Email already exists.";

            using var sha256 = SHA256.Create();

            var passwordBytes = Encoding.UTF8.GetBytes(dto.Password);
            var hashBytes = sha256.ComputeHash(passwordBytes);
            var passwordHash = Convert.ToBase64String(hashBytes);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "Customer"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return "Registration successful.";
        }

        public async Task<User?> ValidateUser(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return null;

            using var sha256 = SHA256.Create();

            var passwordBytes = Encoding.UTF8.GetBytes(dto.Password);
            var hashBytes = sha256.ComputeHash(passwordBytes);
            var passwordHash = Convert.ToBase64String(hashBytes);
            Console.WriteLine($"Stored: {user.PasswordHash}");
            Console.WriteLine($"Generated: {passwordHash}");
            if (user.PasswordHash != passwordHash)
                return null;

            return user;
        }
    }
}