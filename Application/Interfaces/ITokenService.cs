using Application.Common.Security;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user);
        (string Token, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken();
        string HashOpaqueToken(string token);
        TokenValidationResult ValidateAccessToken(string token);
    }
}
