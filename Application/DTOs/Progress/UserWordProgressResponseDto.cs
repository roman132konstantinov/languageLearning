using Domain.Enums;

namespace Application.DTOs.Progress
{
    public class UserWordProgressResponseDto
    {
        public int WordId { get; set; }
        public string KazakhText { get; set; } = string.Empty;
        public string RussianTranslation { get; set; } = string.Empty;
        public WordProgressStatus Status { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public DateTime? LastReviewedAt { get; set; }
        public DateTime? NextReviewAt { get; set; }
        public double EaseFactor { get; set; }
        public int RepetitionCount { get; set; }
    }
}
