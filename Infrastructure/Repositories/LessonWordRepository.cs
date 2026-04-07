using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories
{
    public class LessonWordRepository : ILessonWordRepository
    {
        private readonly LanguageLearningDbContext _context;

        public LessonWordRepository(LanguageLearningDbContext context)
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

        public async Task<bool> ExistsAsync(int lessonId, int wordId)
        {
            return await _context.LessonWords
                .AnyAsync(x => x.LessonId == lessonId && x.WordId == wordId);
        }

        public async Task AddAsync(LessonWord lessonWord)
        {
            await _context.LessonWords.AddAsync(lessonWord);
        }

        public void Delete(LessonWord lessonWord)
        {
            _context.LessonWords.Remove(lessonWord);
        }

        public async Task<LessonWord?> GetByIdsAsync(int lessonId, int wordId)
        {
            return await _context.LessonWords
                .FirstOrDefaultAsync(x => x.LessonId == lessonId && x.WordId == wordId);
        }

        public async Task<List<LessonWord>> GetLessonWordsAsync(int lessonId)
        {
            return await _context.LessonWords
                .AsNoTracking()
                .Include(x => x.Word)
                .Where(x => x.LessonId == lessonId)
                .OrderBy(x => x.Word.KazakhText)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
