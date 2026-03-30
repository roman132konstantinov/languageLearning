using Domain.Enums;

namespace Application.DTOs.Words
{
    public class UpdateWordDto
    {
        public string KazakhText { get; set; } = string.Empty;
        public string RussianTranslation { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? Example { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public LanguageLevel Level { get; set; } = LanguageLevel.A1;
        public bool IsActive { get; set; } = true;
        public int CategoryId { get; set; }
    }
}