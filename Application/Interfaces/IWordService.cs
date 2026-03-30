using Application.DTOs.Words;

namespace Application.Interfaces
{
    public interface IWordService
    {
        Task<List<WordResponseDto>> GetAllAsync();
        Task<WordResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateWordDto dto);
        Task<bool> UpdateAsync(int id, UpdateWordDto dto);
        Task<bool> DeleteAsync(int id);
    }
}