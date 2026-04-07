using Application.DTOs.Lessons;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ILessonService
    {
        Task<List<LessonResponseDto>> GetAllAsync();
        Task<LessonResponseDto?> GetByIdAsync(int id);
        Task<LessonResponseDto> CreateAsync(CreateLessonDto dto);
        Task<LessonResponseDto?> UpdateAsync(int id, UpdateLessonDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
