using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class LessonWord
    {
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public int WordId { get; set; }
        public Word Word { get; set; } = null!;

        public int Order { get; set; }
    }
}
