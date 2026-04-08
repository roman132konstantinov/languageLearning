namespace Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string TokenHash { get; set; } = string.Empty;
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }
        public string? UserAgent { get; set; }
        public string? RevocationReason { get; set; }

        public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
    }
}
