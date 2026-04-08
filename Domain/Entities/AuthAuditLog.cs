using Domain.Enums;

namespace Domain.Entities
{
    public class AuthAuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public AuthAuditEventType EventType { get; set; }
        public string? Email { get; set; }
        public bool IsSuccessful { get; set; }
        public string? FailureReason { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
