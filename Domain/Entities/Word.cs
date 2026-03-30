using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Word
    {
        public int Id { get; set; }

        public string KazakhText { get; set; } = string.Empty;
        public string RussianTranslation { get; set; } = string.Empty;

        public string? Pronunciation { get; set; }
        public string? Example { get; set; }

        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }

        public LanguageLevel Level { get; set; } = LanguageLevel.A1;
        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<LessonWord> LessonWords { get; set; } = new List<LessonWord>();
        public ICollection<UserWordProgress> UserWordProgresses { get; set; } = new List<UserWordProgress>();
    }
}
