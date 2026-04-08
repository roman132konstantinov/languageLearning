using Application.DTOs.Users;

namespace Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public UserResponseDto User { get; set; } = new();
    }
}
