using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WordRepository : IWordRepository
    {
        private readonly LanguageLearningDbContext _db;

        public WordRepository(LanguageLearningDbContext db)
        {
            _db = db;
        }

        public async Task<List<Word>> GetAllAsync()
        {
            return await _db.Words
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Word?> GetByIdAsync(int id)
        {
            return await _db.Words
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Word word)
        {
            await _db.Words.AddAsync(word);
        }

        public void Update(Word word)
        {
            _db.Words.Update(word);
        }

        public void Delete(Word word)
        {
            _db.Words.Remove(word);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}