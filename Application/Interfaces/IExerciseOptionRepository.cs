using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface IExerciseOptionRepository
    {
        Task<List<ExerciseOption>> GetByExerciseIdAsync(int exerciseId);
        Task<ExerciseOption?> GetByIdAsync(int id);
        Task<ExerciseType?> GetExerciseTypeAsync(int exerciseId);
        Task<bool> HasCorrectOptionAsync(int exerciseId, int? excludeOptionId = null);
        Task AddAsync(ExerciseOption option);
        Task UpdateAsync(ExerciseOption option);
        Task DeleteAsync(ExerciseOption option);
        Task<bool> ExerciseExistsAsync(int exerciseId);
        Task SaveChangesAsync();
    }
}
