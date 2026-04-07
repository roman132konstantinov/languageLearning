using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ILessonWordRepository
    {
        Task<bool> LessonExistsAsync(int lessonId);
        Task<bool> WordExistsAsync(int wordId);
        Task<bool> ExistsAsync(int lessonId, int wordId);

        Task AddAsync(LessonWord lessonWord);
        void Delete(LessonWord lessonWord);

        Task<LessonWord?> GetByIdsAsync(int lessonId, int wordId);
        Task<List<LessonWord>> GetLessonWordsAsync(int lessonId);

        Task SaveChangesAsync();
    }
}
