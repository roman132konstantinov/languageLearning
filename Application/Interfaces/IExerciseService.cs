using Application.DTOs.Exercises;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IExerciseService
    {
        Task<List<ExerciseResponseDto>> GetLessonExercisesAsync(int lessonId);
        Task<ExerciseResponseDto?> GetByIdAsync(int id);
        Task<ExerciseResponseDto> CreateAsync(int lessonId, CreateExerciseDto dto);
        Task<ExerciseResponseDto?> UpdateAsync(int id, UpdateExerciseDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
