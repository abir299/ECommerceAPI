using ECommerceAPI.Data.Entities;
using ECommerceAPI.DTOs;

namespace ECommerceAPI.Interfaces
{
    public interface IAuthService
    {
        Task<string> Register(RegisterDto dto);
        Task<User?> ValidateUser(LoginDto dto);
    }
}