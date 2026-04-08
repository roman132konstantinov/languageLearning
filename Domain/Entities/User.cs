using Domain.Enums;

namespace Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public LanguageLevel Level { get; set; } = LanguageLevel.Beginner;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
        public string? EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; set; }
        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        public ICollection<UserWordProgress> WordProgresses { get; set; } = new List<UserWordProgress>();
        public ICollection<UserLessonProgress> UserLessonProgresses { get; set; } = new List<UserLessonProgress>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<AuthAuditLog> AuthAuditLogs { get; set; } = new List<AuthAuditLog>();
    }
}
