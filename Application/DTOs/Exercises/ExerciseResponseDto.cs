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
    }
}
