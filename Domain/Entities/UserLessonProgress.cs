using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class UserLessonProgress
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int Score { get; set; }
    }
}
