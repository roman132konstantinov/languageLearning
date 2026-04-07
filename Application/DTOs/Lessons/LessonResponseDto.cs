using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Lessons
{
    public class LessonResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LanguageLevel Level { get; set; }
        public int Order { get; set; }
        public bool IsPublished { get; set; }
    }
}
