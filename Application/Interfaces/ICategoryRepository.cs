using Application.Common.Pagination;
using Application.DTOs.Category;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<PagedResult<Category>> GetAllAsync(CategoryQueryDto query);
        Task<Category?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeCategoryId = null);
        Task<bool> HasWordsAsync(int id);
        Task AddAsync(Category category);
        void Update(Category category);
        void Delete(Category category);
        Task SaveChangesAsync();
    }
}
