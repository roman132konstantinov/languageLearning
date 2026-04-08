using System.Security.Claims;

namespace Application.Common.Security
{
    public sealed class TokenValidationResult
    {
        public bool Succeeded { get; init; }
        public string? FailureReason { get; init; }
        public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
        public string? SecurityStamp { get; init; }
        public int? UserId { get; init; }
    }
}
