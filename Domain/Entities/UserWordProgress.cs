using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class UserWordProgress
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int WordId { get; set; }
        public Word Word { get; set; } = null!;

        public WordProgressStatus Status { get; set; } = WordProgressStatus.New;

        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }

        public DateTime? LastReviewedAt { get; set; }
        public DateTime? NextReviewAt { get; set; }

        public double EaseFactor { get; set; } = 2.5;
        public int RepetitionCount { get; set; }
    }
}
