using Application.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryResponseDto>> GetAllAsync();
        Task<CategoryResponseDto?> GetByIdAsync(int id);
        Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto);
        Task<bool> UpdateAsync(int id, UpdateCategoryDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
