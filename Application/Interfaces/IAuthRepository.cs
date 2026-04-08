using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> AnyUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdWithRefreshTokensAsync(int userId);
        Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);
        Task<RefreshToken?> GetRefreshTokenByHashWithUserAsync(string tokenHash);
        Task AddUserAsync(User user);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task AddAuditLogAsync(AuthAuditLog auditLog);
        void UpdateUser(User user);
        Task SaveChangesAsync();
    }
}
