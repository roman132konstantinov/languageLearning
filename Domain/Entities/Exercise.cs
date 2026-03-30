using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Exercise
    {
        public int Id { get; set; }

        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public ExerciseType Type { get; set; }

        public string Question { get; set; } = string.Empty;
        public string? Explanation { get; set; }

        public int Order { get; set; }

        public ICollection<ExerciseOption> Options { get; set; } = new List<ExerciseOption>();
    }
}
