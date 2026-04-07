using Application.DTOs.Exercises;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class ExerciseService : IExerciseService
    {
        private readonly IExerciseRepository _exerciseRepository;

        public ExerciseService(IExerciseRepository exerciseRepository)
        {
            _exerciseRepository = exerciseRepository;
        }

        public async Task<List<ExerciseResponseDto>> GetLessonExercisesAsync(int lessonId)
        {
            ValidateLessonId(lessonId);

            var lessonExists = await _exerciseRepository.LessonExistsAsync(lessonId);
            if (!lessonExists)
                throw new ArgumentException("Lesson not found.");

            var exercises = await _exerciseRepository.GetByLessonIdAsync(lessonId);

            return exercises
                .Select(MapToResponse)
                .ToList();
        }

        public async Task<ExerciseResponseDto?> GetByIdAsync(int id)
        {
            ValidateExerciseId(id);

            var exercise = await _exerciseRepository.GetByIdAsync(id);
            if (exercise is null)
                return null;

            return MapToResponse(exercise);
        }

        public async Task<ExerciseResponseDto> CreateAsync(int lessonId, CreateExerciseDto dto)
        {
            ValidateLessonId(lessonId);
            ValidateCreateDto(dto);

            var lessonExists = await _exerciseRepository.LessonExistsAsync(lessonId);
            if (!lessonExists)
                throw new ArgumentException("Lesson not found.");

            var duplicateOrder = await _exerciseRepository.ExistsWithOrderAsync(lessonId, dto.Order);
            if (duplicateOrder)
                throw new ArgumentException("Exercise with this order already exists in the lesson.");

            var exercise = new Exercise
            {
                LessonId = lessonId,
                Question = dto.Question.Trim(),
                Order = dto.Order,
                Type = dto.Type,
                Explanation = string.IsNullOrWhiteSpace(dto.Explanation)
                    ? null
                    : dto.Explanation.Trim()
            };

            await _exerciseRepository.AddAsync(exercise);
            await _exerciseRepository.SaveChangesAsync();

            return MapToResponse(exercise);
        }

        public async Task<ExerciseResponseDto?> UpdateAsync(int id, UpdateExerciseDto dto)
        {
            ValidateExerciseId(id);
            ValidateUpdateDto(dto);

            var exercise = await _exerciseRepository.GetByIdWithLessonAsync(id);
            if (exercise is null)
                return null;

            var duplicateOrder = await _exerciseRepository.ExistsWithOrderAsync(
                exercise.LessonId,
                dto.Order,
                exercise.Id);

            if (duplicateOrder)
                throw new ArgumentException("Exercise with this order already exists in the lesson.");

            exercise.Question = dto.Question.Trim();
            exercise.Order = dto.Order;
            exercise.Type = dto.Type;
            exercise.Explanation = string.IsNullOrWhiteSpace(dto.Explanation)
                ? null
                : dto.Explanation.Trim();

            _exerciseRepository.Update(exercise);
            await _exerciseRepository.SaveChangesAsync();

            return MapToResponse(exercise);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            ValidateExerciseId(id);

            var exercise = await _exerciseRepository.GetByIdWithLessonAsync(id);
            if (exercise is null)
                return false;

            _exerciseRepository.Delete(exercise);
            await _exerciseRepository.SaveChangesAsync();

            return true;
        }

        private static ExerciseResponseDto MapToResponse(Exercise exercise)
        {
            return new ExerciseResponseDto
            {
                Id = exercise.Id,
                LessonId = exercise.LessonId,
                Question = exercise.Question,
                Order = exercise.Order,
                Type = exercise.Type,
                Explanation = exercise.Explanation
            };
        }

        private static void ValidateLessonId(int lessonId)
        {
            if (lessonId <= 0)
                throw new ArgumentException("LessonId must be greater than 0.");
        }

        private static void ValidateExerciseId(int exerciseId)
        {
            if (exerciseId <= 0)
                throw new ArgumentException("ExerciseId must be greater than 0.");
        }

        private static void ValidateCreateDto(CreateExerciseDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Question))
                throw new ArgumentException("Question is required.");

            if (dto.Question.Trim().Length > 500)
                throw new ArgumentException("Question must not exceed 500 characters.");

            if (dto.Order <= 0)
                throw new ArgumentException("Order must be greater than 0.");

            if (!Enum.IsDefined(typeof(ExerciseType), dto.Type))
                throw new ArgumentException("Invalid exercise type.");

            if (dto.Explanation is not null && dto.Explanation.Trim().Length > 1000)
                throw new ArgumentException("Explanation must not exceed 1000 characters.");
        }

        private static void ValidateUpdateDto(UpdateExerciseDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Question))
                throw new ArgumentException("Question is required.");

            if (dto.Question.Trim().Length > 500)
                throw new ArgumentException("Question must not exceed 500 characters.");

            if (dto.Order <= 0)
                throw new ArgumentException("Order must be greater than 0.");

            if (!Enum.IsDefined(typeof(ExerciseType), dto.Type))
                throw new ArgumentException("Invalid exercise type.");

            if (dto.Explanation is not null && dto.Explanation.Trim().Length > 1000)
                throw new ArgumentException("Explanation must not exceed 1000 characters.");
        }
    }
}