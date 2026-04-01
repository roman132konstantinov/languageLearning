using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private LanguageLearningDbContext _db;
        public CategoryRepository(LanguageLearningDbContext db)
        {
            _db = db;

        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

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
