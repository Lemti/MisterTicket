using MisterTicket.Api.DTOs;
using MisterTicket.Api.Models;

namespace MisterTicket.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> Register(RegisterDto registerDto);
        Task<AuthResponseDto?> Login(LoginDto loginDto);
        string GenerateJwtToken(User user);
    }
}