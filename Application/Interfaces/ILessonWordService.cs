using Application.DTOs.LessonWords;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ILessonWordService
    {
        Task<List<LessonWordResponseDto>> GetLessonWordsAsync(int lessonId);
        Task AddWordToLessonAsync(int lessonId, AddWordToLessonDto dto);
        Task<bool> RemoveWordFromLessonAsync(int lessonId, int wordId);
    }
}
