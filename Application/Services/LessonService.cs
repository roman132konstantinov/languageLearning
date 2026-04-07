using Application.DTOs.Lessons;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _lessonRepository;

        public LessonService(ILessonRepository lessonRepository)
        {
            _lessonRepository = lessonRepository;
        }

        public async Task<List<LessonResponseDto>> GetAllAsync()
        {
            var lessons = await _lessonRepository.GetAllAsync();

            return lessons
                .Select(MapToResponse)
                .ToList();
        }

        public async Task<LessonResponseDto?> GetByIdAsync(int id)
        {
            var lesson = await _lessonRepository.GetByIdAsync(id);

            if (lesson is null)
                return null;

            return MapToResponse(lesson);
        }

        public async Task<LessonResponseDto> CreateAsync(CreateLessonDto dto)
        {
            ValidateCreateDto(dto);

            var lesson = new Lesson
            {
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim(),
                Level = dto.Level,
                Order = dto.Order,
                IsPublished = dto.IsPublished
            };

            await _lessonRepository.AddAsync(lesson);
            await _lessonRepository.SaveChangesAsync();

            return MapToResponse(lesson);
        }

        public async Task<LessonResponseDto?> UpdateAsync(int id, UpdateLessonDto dto)
        {
            ValidateUpdateDto(dto);

            var lesson = await _lessonRepository.GetByIdAsync(id);

            if (lesson is null)
                return null;

            lesson.Title = dto.Title.Trim();
            lesson.Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim();
            lesson.Level = dto.Level;
            lesson.Order = dto.Order;
            lesson.IsPublished = dto.IsPublished;

            _lessonRepository.Update(lesson);
            await _lessonRepository.SaveChangesAsync();

            return MapToResponse(lesson);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var lesson = await _lessonRepository.GetByIdAsync(id);

            if (lesson is null)
                return false;

            _lessonRepository.Delete(lesson);
            await _lessonRepository.SaveChangesAsync();

            return true;
        }

        private static LessonResponseDto MapToResponse(Lesson lesson)
        {
            return new LessonResponseDto
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Description = lesson.Description,
                Level = lesson.Level,
                Order = lesson.Order,
                IsPublished = lesson.IsPublished
            };
        }

        private static void ValidateCreateDto(CreateLessonDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Lesson title is required.");

            if (dto.Title.Trim().Length > 200)
                throw new ArgumentException("Lesson title must not exceed 200 characters.");

            if (dto.Description is not null && dto.Description.Length > 1000)
                throw new ArgumentException("Lesson description must not exceed 1000 characters.");

            if (dto.Order < 1)
                throw new ArgumentException("Lesson order must be greater than 0.");
        }

        private static void ValidateUpdateDto(UpdateLessonDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Lesson title is required.");

            if (dto.Title.Trim().Length > 200)
                throw new ArgumentException("Lesson title must not exceed 200 characters.");

            if (dto.Description is not null && dto.Description.Length > 1000)
                throw new ArgumentException("Lesson description must not exceed 1000 characters.");

            if (dto.Order < 1)
                throw new ArgumentException("Lesson order must be greater than 0.");
        }
    }
}