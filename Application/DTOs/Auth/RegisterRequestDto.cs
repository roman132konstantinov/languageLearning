using Domain.Enums;

namespace Application.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public LanguageLevel Level { get; set; } = LanguageLevel.A1;
    }
}
