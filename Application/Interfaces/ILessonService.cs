using Application.DTOs.Common;
using Application.DTOs.Lessons;

namespace Application.Interfaces
{
    public interface ILessonService
    {
        Task<PagedResponseDto<LessonResponseDto>> GetAllAsync(LessonQueryDto query);
        Task<LessonResponseDto?> GetByIdAsync(int id);
        Task<LessonResponseDto> CreateAsync(CreateLessonDto dto);
        Task<LessonResponseDto?> UpdateAsync(int id, UpdateLessonDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
