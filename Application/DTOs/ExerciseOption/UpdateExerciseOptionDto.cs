using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.ExerciseOption
{
    public class UpdateExerciseOptionDto
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
