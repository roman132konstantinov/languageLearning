using Application.DTOs.Category;
using Application.DTOs.Common;

namespace Application.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedResponseDto<CategoryResponseDto>> GetAllAsync(CategoryQueryDto query);
        Task<CategoryResponseDto?> GetByIdAsync(int id);
        Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto);
        Task<bool> UpdateAsync(int id, UpdateCategoryDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
