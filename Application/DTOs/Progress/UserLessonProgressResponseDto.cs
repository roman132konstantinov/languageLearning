namespace Application.DTOs.Progress
{
    public class UserLessonProgressResponseDto
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int Score { get; set; }
        public int LearnedWords { get; set; }
        public int TotalWords { get; set; }
    }
}
