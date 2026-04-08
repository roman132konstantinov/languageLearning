using Application.DTOs.ExerciseOption;
using Application.DTOs.Exercises;
using Application.Interfaces;
using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class ExerciseService : IExerciseService
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IUserWordProgressService _userWordProgressService;
        private readonly IUserLessonProgressService _userLessonProgressService;

        public ExerciseService(
            IExerciseRepository exerciseRepository,
            IUserWordProgressService userWordProgressService,
            IUserLessonProgressService userLessonProgressService)
        {
            _exerciseRepository = exerciseRepository;
            _userWordProgressService = userWordProgressService;
            _userLessonProgressService = userLessonProgressService;
        }

        public async Task<List<ExerciseResponseDto>> GetLessonExercisesAsync(int lessonId)
        {
            ValidateLessonId(lessonId);

            var lessonExists = await _exerciseRepository.LessonExistsAsync(lessonId);
            if (!lessonExists)
                throw new NotFoundException("Lesson not found.");

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
                throw new NotFoundException("Lesson not found.");

            var duplicateOrder = await _exerciseRepository.ExistsWithOrderAsync(lessonId, dto.Order);
            if (duplicateOrder)
                throw new ConflictException("Exercise with this order already exists in the lesson.");

            if (dto.WordId.HasValue)
            {
                var wordExists = await _exerciseRepository.WordExistsAsync(dto.WordId.Value);
                if (!wordExists)
                    throw new NotFoundException("Word not found.");
            }

            var exercise = new Exercise
            {
                LessonId = lessonId,
                Question = dto.Question.Trim(),
                Order = dto.Order,
                Type = dto.Type,
                WordId = dto.WordId,
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
                throw new ConflictException("Exercise with this order already exists in the lesson.");

            if (dto.WordId.HasValue)
            {
                var wordExists = await _exerciseRepository.WordExistsAsync(dto.WordId.Value);
                if (!wordExists)
                    throw new NotFoundException("Word not found.");
            }

            exercise.Question = dto.Question.Trim();
            exercise.Order = dto.Order;
            exercise.Type = dto.Type;
            exercise.WordId = dto.WordId;
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

        public async Task<SubmitAnswerResultDto> SubmitAnswerAsync(int exerciseId, SubmitAnswerDto dto)
        {
            ValidateExerciseId(exerciseId);

            if (dto is null)
                throw new ValidationException("Submit answer payload is required.");

            if (dto.OptionId <= 0)
                throw new ValidationException("OptionId must be greater than 0.");

            if (dto.UserId.HasValue && dto.UserId.Value <= 0)
                throw new ValidationException("UserId must be greater than 0.");

            var exercise = await _exerciseRepository.GetByIdAsync(exerciseId);

            if (exercise == null)
                throw new NotFoundException("Exercise not found.");

            if (exercise.Type != ExerciseType.ChooseAnsver)
                throw new ValidationException("This exercise does not support options.");

            var selectedOption = exercise.Options
                .FirstOrDefault(x => x.Id == dto.OptionId);

            if (selectedOption == null)
                throw new NotFoundException("Option not found.");

            var correctOptions = exercise.Options
                .Where(x => x.IsCorrect)
                .ToList();

            if (correctOptions.Count != 1)
                throw new ConflictException("Exercise must have exactly one correct option configured.");

            var isCorrect = selectedOption.IsCorrect;
            bool? isLessonCompleted = null;
            int? lessonScore = null;

            if (exercise.WordId.HasValue && dto.UserId.HasValue)
            {
                await _userWordProgressService.UpdateAsync(dto.UserId.Value, exercise.WordId.Value, isCorrect);
                var lessonProgress = await _userLessonProgressService.RecalculateAsync(dto.UserId.Value, exercise.LessonId);
                isLessonCompleted = lessonProgress.IsCompleted;
                lessonScore = lessonProgress.Score;
            }

            return new SubmitAnswerResultDto
            {
                IsCorrect = isCorrect,
                CorrectAnswer = isCorrect ? null : correctOptions[0].Text,
                Explanation = exercise.Explanation,
                IsLessonCompleted = isLessonCompleted,
                LessonScore = lessonScore
            };
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
                Explanation = exercise.Explanation,
                WordId = exercise.WordId
            };
        }

        private static void ValidateLessonId(int lessonId)
        {
            if (lessonId <= 0)
                throw new ValidationException("LessonId must be greater than 0.");
        }

        private static void ValidateExerciseId(int exerciseId)
        {
            if (exerciseId <= 0)
                throw new ValidationException("ExerciseId must be greater than 0.");
        }

        private static void ValidateCreateDto(CreateExerciseDto dto)
        {
            if (dto is null)
                throw new ValidationException("Exercise payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Question))
                throw new ValidationException("Question is required.");

            if (dto.Question.Trim().Length > 500)
                throw new ValidationException("Question must not exceed 500 characters.");

            if (dto.Order <= 0)
                throw new ValidationException("Order must be greater than 0.");

            if (!Enum.IsDefined(typeof(ExerciseType), dto.Type))
                throw new ValidationException("Invalid exercise type.");

            if (dto.Explanation is not null && dto.Explanation.Trim().Length > 1000)
                throw new ValidationException("Explanation must not exceed 1000 characters.");

            if (dto.WordId.HasValue && dto.WordId.Value <= 0)
                throw new ValidationException("WordId must be greater than 0.");
        }

        private static void ValidateUpdateDto(UpdateExerciseDto dto)
        {
            if (dto is null)
                throw new ValidationException("Exercise payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Question))
                throw new ValidationException("Question is required.");

            if (dto.Question.Trim().Length > 500)
                throw new ValidationException("Question must not exceed 500 characters.");

            if (dto.Order <= 0)
                throw new ValidationException("Order must be greater than 0.");

            if (!Enum.IsDefined(typeof(ExerciseType), dto.Type))
                throw new ValidationException("Invalid exercise type.");

            if (dto.Explanation is not null && dto.Explanation.Trim().Length > 1000)
                throw new ValidationException("Explanation must not exceed 1000 characters.");

            if (dto.WordId.HasValue && dto.WordId.Value <= 0)
                throw new ValidationException("WordId must be greater than 0.");
        }
    }
}
