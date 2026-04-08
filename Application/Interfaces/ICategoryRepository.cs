using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeCategoryId = null);
        Task<bool> HasWordsAsync(int id);
        Task AddAsync(Category category);
        void Update(Category category);
        void Delete(Category category);
        Task SaveChangesAsync();
    }
}
