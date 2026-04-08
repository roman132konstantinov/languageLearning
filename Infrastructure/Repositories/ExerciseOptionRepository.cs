using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ExerciseOptionRepository : IExerciseOptionRepository
    {
        private readonly LanguageLearningDbContext _context;

        public ExerciseOptionRepository(LanguageLearningDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExerciseOption>> GetByExerciseIdAsync(int exerciseId)
        {
            return await _context.ExerciseOptions
                .Where(x => x.ExerciseId == exerciseId)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<ExerciseOption?> GetByIdAsync(int id)
        {
            return await _context.ExerciseOptions
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ExerciseType?> GetExerciseTypeAsync(int exerciseId)
        {
            return await _context.Exercises
                .Where(x => x.Id == exerciseId)
                .Select(x => (ExerciseType?)x.Type)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasCorrectOptionAsync(int exerciseId, int? excludeOptionId = null)
        {
            return await _context.ExerciseOptions
                .AnyAsync(x => x.ExerciseId == exerciseId
                    && x.IsCorrect
                    && (!excludeOptionId.HasValue || x.Id != excludeOptionId.Value));
        }

        public async Task AddAsync(ExerciseOption option)
        {
            await _context.ExerciseOptions.AddAsync(option);
        }

        public Task UpdateAsync(ExerciseOption option)
        {
            _context.ExerciseOptions.Update(option);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ExerciseOption option)
        {
            _context.ExerciseOptions.Remove(option);
            return Task.CompletedTask;
        }

        public async Task<bool> ExerciseExistsAsync(int exerciseId)
        {
            return await _context.Exercises.AnyAsync(x => x.Id == exerciseId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
