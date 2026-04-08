using Domain.Enums;

namespace Application.DTOs.Users
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public LanguageLevel Level { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
