using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly LanguageLearningDbContext _context;

        public AuthRepository(LanguageLearningDbContext context)
        {
            _context = context;
        }

        public Task<bool> AnyUsersAsync()
        {
            return _context.Users.AnyAsync();
        }

        public Task<User?> GetUserByIdAsync(int userId)
        {
            return _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            return _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public Task<User?> GetUserByIdWithRefreshTokensAsync(int userId)
        {
            return _context.Users
                .Include(x => x.RefreshTokens)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
        {
            return _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        }

        public Task<RefreshToken?> GetRefreshTokenByHashWithUserAsync(string tokenHash)
        {
            return _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        }

        public Task AddUserAsync(User user)
        {
            return _context.Users.AddAsync(user).AsTask();
        }

        public Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            return _context.RefreshTokens.AddAsync(refreshToken).AsTask();
        }

        public Task AddAuditLogAsync(AuthAuditLog auditLog)
        {
            return _context.AuthAuditLogs.AddAsync(auditLog).AsTask();
        }

        public void UpdateUser(User user)
        {
            _context.Users.Update(user);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
