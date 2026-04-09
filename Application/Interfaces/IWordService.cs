using Application.DTOs.Common;
using Application.DTOs.Words;

namespace Application.Interfaces
{
    public interface IWordService
    {
        Task<PagedResponseDto<WordResponseDto>> GetAllAsync(WordQueryDto query);
        Task<WordResponseDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateWordDto dto);
        Task<bool> UpdateAsync(int id, UpdateWordDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
