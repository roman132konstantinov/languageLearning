using Application.Common.Pagination;
using Application.DTOs.Words;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IWordRepository
    {
        Task<PagedResult<Word>> GetAllAsync(WordQueryDto query);
        Task<Word?> GetByIdAsync(int id);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task AddAsync(Word word);
        void Update(Word word);
        void Delete(Word word);
        Task SaveChangesAsync();
    }
}
