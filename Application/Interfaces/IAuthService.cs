using Application.DTOs.Auth;
using Application.DTOs.Users;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, AuthRequestMetadata metadata);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, AuthRequestMetadata metadata);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, AuthRequestMetadata metadata);
        Task LogoutAsync(int userId, LogoutRequestDto dto, AuthRequestMetadata metadata);
        Task<UserResponseDto> GetCurrentUserAsync(int userId);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto, AuthRequestMetadata metadata);
    }
}
