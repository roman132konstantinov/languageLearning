using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly LanguageLearningDbContext _db;

        public CategoryRepository(LanguageLearningDbContext db)
        {
            _db = db;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeCategoryId = null)
        {
            var normalized = name.Trim().ToLower();

            return await _db.Categories
                .AnyAsync(x => x.Name.ToLower() == normalized
                    && (!excludeCategoryId.HasValue || x.Id != excludeCategoryId.Value));
        }

        public async Task<bool> HasWordsAsync(int id)
        {
            return await _db.Words.AnyAsync(x => x.CategoryId == id);
        }

        public async Task AddAsync(Category category)
        {
            await _db.Categories.AddAsync(category);
        }

        public void Update(Category category)
        {
            _db.Categories.Update(category);
        }

        public void Delete(Category category)
        {
            _db.Categories.Remove(category);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
