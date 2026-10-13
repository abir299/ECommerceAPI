using ECommerceAPI.DTOs;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(
        AuthService authService,
        JwtService jwtService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await authService.Register(dto);

            if (result != "Registration successful.")
                return BadRequest(new { message = result });

            return Ok(new { message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await authService.ValidateUser(dto);

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            var token = jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful.",
                token = token,
                role = user.Role
            });
        }
    }
}