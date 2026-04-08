using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserLessonProgressRepository : IUserLessonProgressRepository
    {
        private readonly LanguageLearningDbContext _context;

        public UserLessonProgressRepository(LanguageLearningDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            return await _context.Users.AnyAsync(x => x.Id == userId);
        }

        public async Task<bool> LessonExistsAsync(int lessonId)
        {
            return await _context.Lessons.AnyAsync(x => x.Id == lessonId);
        }

        public async Task<UserLessonProgress?> GetByUserAndLessonAsync(int userId, int lessonId)
        {
            return await _context.UserLessonProgresses
                .Include(x => x.Lesson)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId);
        }

        public async Task<List<UserLessonProgress>> GetByUserAsync(int userId)
        {
            return await _context.UserLessonProgresses
                .AsNoTracking()
                .Include(x => x.Lesson)
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Lesson.Order)
                .ToListAsync();
        }

        public async Task<int> GetLessonWordCountAsync(int lessonId)
        {
            return await _context.LessonWords
                .CountAsync(x => x.LessonId == lessonId);
        }

        public async Task<int> GetLearnedLessonWordCountAsync(int userId, int lessonId)
        {
            return await _context.LessonWords
                .Where(lw => lw.LessonId == lessonId)
                .Join(
                    _context.UserWordProgresses.Where(uwp => uwp.UserId == userId && uwp.CorrectAnswers > 0),
                    lw => lw.WordId,
                    uwp => uwp.WordId,
                    (lw, _) => lw.WordId)
                .Distinct()
                .CountAsync();
        }

        public async Task AddAsync(UserLessonProgress progress)
        {
            await _context.UserLessonProgresses.AddAsync(progress);
        }

        public void Update(UserLessonProgress progress)
        {
            _context.UserLessonProgresses.Update(progress);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
