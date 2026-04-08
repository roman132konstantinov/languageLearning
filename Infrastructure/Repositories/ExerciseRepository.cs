using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ExerciseRepository : IExerciseRepository
    {
        private readonly LanguageLearningDbContext _context;

        public ExerciseRepository(LanguageLearningDbContext context)
        {
            _context = context;
        }

        public async Task<bool> LessonExistsAsync(int lessonId)
        {
            return await _context.Lessons.AnyAsync(x => x.Id == lessonId);
        }

        public async Task<bool> WordExistsAsync(int wordId)
        {
            return await _context.Words.AnyAsync(x => x.Id == wordId);
        }

        public async Task<List<Exercise>> GetByLessonIdAsync(int lessonId)
        {
            return await _context.Exercises
                .AsNoTracking()
                .Where(x => x.LessonId == lessonId)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        public async Task<Exercise?> GetByIdAsync(int id)
        {
            return await _context.Exercises
                .Include(x => x.Options)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Exercise?> GetByIdWithLessonAsync(int id)
        {
            return await _context.Exercises
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Exercise exercise)
        {
            await _context.Exercises.AddAsync(exercise);
        }

        public void Update(Exercise exercise)
        {
            _context.Exercises.Update(exercise);
        }

        public void Delete(Exercise exercise)
        {
            _context.Exercises.Remove(exercise);
        }

        public async Task<bool> ExistsWithOrderAsync(int lessonId, int order)
        {
            return await _context.Exercises
                .AnyAsync(x => x.LessonId == lessonId && x.Order == order);
        }

        public async Task<bool> ExistsWithOrderAsync(int lessonId, int order, int excludeExerciseId)
        {
            return await _context.Exercises
                .AnyAsync(x => x.LessonId == lessonId
                            && x.Order == order
                            && x.Id != excludeExerciseId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
