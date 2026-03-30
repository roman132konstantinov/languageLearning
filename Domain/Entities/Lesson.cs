using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Lesson
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public LanguageLevel Level { get; set; } = LanguageLevel.A1;
        public int Order { get; set; }

        public bool IsPublished { get; set; } = true;

        public ICollection<LessonWord> LessonWords { get; set; } = new List<LessonWord>();
        public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
        public ICollection<UserLessonProgress> UserLessonProgresses { get; set; } = new List<UserLessonProgress>();
    }
}
