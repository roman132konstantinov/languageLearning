using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.ExerciseOption
{
    public class ExerciseOptionResponseDto
    {
        public int Id { get; set; }
        public int ExerciseId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
