using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Lessons
{
    public class CreateLessonDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LanguageLevel Level { get; set; } = LanguageLevel.A1;
        public int Order { get; set; }
        public bool IsPublished { get; set; } = true;
    }
}
