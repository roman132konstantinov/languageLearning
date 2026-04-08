using Application.DTOs.ExerciseOption;
using Application.Interfaces;
using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class ExerciseOptionService : IExerciseOptionService
    {
        private readonly IExerciseOptionRepository _exerciseOptionRepository;

        public ExerciseOptionService(IExerciseOptionRepository exerciseOptionRepository)
        {
            _exerciseOptionRepository = exerciseOptionRepository;
        }

        public async Task<List<ExerciseOptionResponseDto>> GetByExerciseIdAsync(int exerciseId)
        {
            ValidateExerciseId(exerciseId);

            if (!await _exerciseOptionRepository.ExerciseExistsAsync(exerciseId))
                throw new NotFoundException("Exercise not found.");

            var options = await _exerciseOptionRepository.GetByExerciseIdAsync(exerciseId);

            return options.Select(MapToDto).ToList();
        }

        public async Task<ExerciseOptionResponseDto?> GetByIdAsync(int id)
        {
            ValidateOptionId(id);

            var option = await _exerciseOptionRepository.GetByIdAsync(id);
            if (option == null)
                return null;

            return MapToDto(option);
        }

        public async Task<ExerciseOptionResponseDto> CreateAsync(int exerciseId, CreateExerciseOptionDto dto)
        {
            ValidateExerciseId(exerciseId);
            ValidateCreateDto(dto);

            var exerciseType = await _exerciseOptionRepository.GetExerciseTypeAsync(exerciseId);
            if (exerciseType is null)
                throw new NotFoundException("Exercise not found.");

            if (exerciseType != ExerciseType.ChooseAnsver)
                throw new ValidationException("Options are supported only for choose-answer exercises.");

            if (dto.IsCorrect && await _exerciseOptionRepository.HasCorrectOptionAsync(exerciseId))
                throw new ConflictException("Only one correct option is allowed for an exercise.");

            var option = new ExerciseOption
            {
                ExerciseId = exerciseId,
                Text = dto.Text.Trim(),
                IsCorrect = dto.IsCorrect
            };

            await _exerciseOptionRepository.AddAsync(option);
            await _exerciseOptionRepository.SaveChangesAsync();

            return MapToDto(option);
        }

        public async Task<bool> UpdateAsync(int id, UpdateExerciseOptionDto dto)
        {
            ValidateOptionId(id);
            ValidateUpdateDto(dto);

            var option = await _exerciseOptionRepository.GetByIdAsync(id);
            if (option == null)
                return false;

            var exerciseType = await _exerciseOptionRepository.GetExerciseTypeAsync(option.ExerciseId);
            if (exerciseType != ExerciseType.ChooseAnsver)
                throw new ValidationException("Options are supported only for choose-answer exercises.");

            if (dto.IsCorrect && await _exerciseOptionRepository.HasCorrectOptionAsync(option.ExerciseId, id))
                throw new ConflictException("Only one correct option is allowed for an exercise.");

            option.Text = dto.Text.Trim();
            option.IsCorrect = dto.IsCorrect;

            await _exerciseOptionRepository.UpdateAsync(option);
            await _exerciseOptionRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            ValidateOptionId(id);

            var option = await _exerciseOptionRepository.GetByIdAsync(id);
            if (option == null)
                return false;

            await _exerciseOptionRepository.DeleteAsync(option);
            await _exerciseOptionRepository.SaveChangesAsync();

            return true;
        }

        private static ExerciseOptionResponseDto MapToDto(ExerciseOption option)
        {
            return new ExerciseOptionResponseDto
            {
                Id = option.Id,
                ExerciseId = option.ExerciseId,
                Text = option.Text,
                IsCorrect = option.IsCorrect
            };
        }

        private static void ValidateExerciseId(int exerciseId)
        {
            if (exerciseId <= 0)
                throw new ValidationException("ExerciseId must be greater than 0.");
        }

        private static void ValidateOptionId(int id)
        {
            if (id <= 0)
                throw new ValidationException("OptionId must be greater than 0.");
        }

        private static void ValidateCreateDto(CreateExerciseOptionDto dto)
        {
            if (dto is null)
                throw new ValidationException("Exercise option payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ValidationException("Option text is required.");

            if (dto.Text.Trim().Length > 300)
                throw new ValidationException("Option text must not exceed 300 characters.");
        }

        private static void ValidateUpdateDto(UpdateExerciseOptionDto dto)
        {
            if (dto is null)
                throw new ValidationException("Exercise option payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ValidationException("Option text is required.");

            if (dto.Text.Trim().Length > 300)
                throw new ValidationException("Option text must not exceed 300 characters.");
        }
    }
}
