using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class LessonRepository : ILessonRepository
    {
        private readonly LanguageLearningDbContext _db;

        public LessonRepository(LanguageLearningDbContext db)
        {
            _db = db;
        }

        public async Task<List<Lesson>> GetAllAsync()
        {
            return await _db.Lessons
                .AsNoTracking()
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        public async Task<Lesson?> GetByIdAsync(int id)
        {
            return await _db.Lessons
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Lesson lesson)
        {
            await _db.Lessons.AddAsync(lesson);
        }

        public void Update(Lesson lesson)
        {
            _db.Lessons.Update(lesson);
        }

        public void Delete(Lesson lesson)
        {
            _db.Lessons.Remove(lesson);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
