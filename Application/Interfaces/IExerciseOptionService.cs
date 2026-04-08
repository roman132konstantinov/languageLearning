using Application.DTOs.ExerciseOption;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IExerciseOptionService
    {
        Task<List<ExerciseOptionResponseDto>> GetByExerciseIdAsync(int exerciseId);
        Task<ExerciseOptionResponseDto?> GetByIdAsync(int id);
        Task<ExerciseOptionResponseDto> CreateAsync(int exerciseId, CreateExerciseOptionDto dto);
        Task<bool> UpdateAsync(int id, UpdateExerciseOptionDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
