using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Exercises
{
    public class CreateExerciseDto
    {
        public string Question { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
