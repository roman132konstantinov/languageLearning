using API.Security;
using Application.DTOs.Auth;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    [ApiController]
    [EnableRateLimiting("auth")]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var response = await _authService.RegisterAsync(dto, BuildMetadata());
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var response = await _authService.LoginAsync(dto, BuildMetadata());
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var response = await _authService.RefreshTokenAsync(dto, BuildMetadata());
            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            await _authService.LogoutAsync(User.GetRequiredUserId(), dto, BuildMetadata());
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _authService.GetCurrentUserAsync(User.GetRequiredUserId());
            return Ok(user);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            await _authService.ChangePasswordAsync(User.GetRequiredUserId(), dto, BuildMetadata());
            return NoContent();
        }

        private AuthRequestMetadata BuildMetadata()
        {
            return new AuthRequestMetadata
            {
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };
        }
    }
}
