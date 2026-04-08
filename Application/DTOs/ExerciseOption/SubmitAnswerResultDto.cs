using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.ExerciseOption
{
    public class SubmitAnswerResultDto
    {
        public bool IsCorrect { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public bool? IsLessonCompleted { get; set; }
        public int? LessonScore { get; set; }
    }
}
