using Domain.Entities;

namespace Application.Interfaces
{
    public interface IWordRepository
    {
        Task<List<Word>> GetAllAsync();
        Task<Word?> GetByIdAsync(int id);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task AddAsync(Word word);
        void Update(Word word);
        void Delete(Word word);
        Task SaveChangesAsync();
    }
}
