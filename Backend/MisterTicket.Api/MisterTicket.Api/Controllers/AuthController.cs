using Microsoft.AspNetCore.Mvc;
using MisterTicket.Api.DTOs;
using MisterTicket.Api.Services;

namespace MisterTicket.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.Register(registerDto);

            if (result == null)
            {
                return BadRequest(new { message = "Email already exists" });
            }

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.Login(loginDto);

            if (result == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            return Ok(result);
        }
    }
}