using Domain.Enums;

namespace Application.DTOs.Users
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public LanguageLevel Level { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }
    }
}
