using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.LessonWords
{
    public class LessonWordResponseDto
    {
        public int WordId { get; set; }
        public int Order { get; set; }
        public string KazakhText { get; set; } = string.Empty;
        public string RussianTranslation { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? Example { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
    }
}
