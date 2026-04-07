using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IExerciseRepository
    {
        Task<bool> LessonExistsAsync(int lessonId);
        Task<List<Exercise>> GetByLessonIdAsync(int lessonId);
        Task<Exercise?> GetByIdAsync(int id);
        Task<Exercise?> GetByIdWithLessonAsync(int id);

        Task AddAsync(Exercise exercise);
        void Update(Exercise exercise);
        void Delete(Exercise exercise);

        Task<bool> ExistsWithOrderAsync(int lessonId, int order);
        Task<bool> ExistsWithOrderAsync(int lessonId, int order, int excludeExerciseId);

        Task SaveChangesAsync();
    }
}
