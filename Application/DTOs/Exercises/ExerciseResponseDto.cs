using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Exercises
{
    public class ExerciseResponseDto
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string Question { get; set; } = string.Empty;
        public int Order { get; set; }
        public ExerciseType Type { get; set; }
        public string? Explanation { get; set; }
        public int? WordId { get; set; }
    }
}
