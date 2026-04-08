using Domain.Enums;

namespace Application.DTOs.Users
{
    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public LanguageLevel Level { get; set; } = LanguageLevel.A1;
        public UserRole Role { get; set; } = UserRole.User;
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
