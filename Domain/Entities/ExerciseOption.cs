using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ExerciseOption
    {
        public int Id { get; set; }

        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
