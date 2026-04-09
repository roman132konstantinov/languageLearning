using Application.Common.Exceptions;
using Application.Common.Pagination;
using Application.DTOs.Common;
using Application.DTOs.Lessons;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _lessonRepository;

        public LessonService(ILessonRepository lessonRepository)
        {
            _lessonRepository = lessonRepository;
        }

        public async Task<PagedResponseDto<LessonResponseDto>> GetAllAsync(LessonQueryDto query)
        {
            query ??= new LessonQueryDto();

            PagedQueryNormalizer.Normalize(query);

            var lessons = await _lessonRepository.GetAllAsync(query);

            return new PagedResponseDto<LessonResponseDto>
            {
                Items = lessons.Items.Select(MapToResponse).ToList(),
                Page = lessons.Page,
                PageSize = lessons.PageSize,
                TotalCount = lessons.TotalCount,
                TotalPages = lessons.TotalPages
            };
        }

        public async Task<LessonResponseDto?> GetByIdAsync(int id)
        {
            ValidateLessonId(id);

            var lesson = await _lessonRepository.GetByIdAsync(id);
            return lesson is null ? null : MapToResponse(lesson);
        }

        public async Task<LessonResponseDto> CreateAsync(CreateLessonDto dto)
        {
            ValidateCreateDto(dto);

            if (await _lessonRepository.ExistsWithOrderAsync(dto.Order))
                throw new ConflictException("Lesson with this order already exists.");

            var lesson = new Lesson
            {
                Title = dto.Title.Trim(),
                Description = NormalizeOptional(dto.Description),
                AudioUrl = NormalizeOptional(dto.AudioUrl),
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
            ValidateLessonId(id);
            ValidateUpdateDto(dto);

            var lesson = await _lessonRepository.GetByIdAsync(id);
            if (lesson is null)
                return null;

            if (await _lessonRepository.ExistsWithOrderAsync(dto.Order, id))
                throw new ConflictException("Lesson with this order already exists.");

            lesson.Title = dto.Title.Trim();
            lesson.Description = NormalizeOptional(dto.Description);
            lesson.AudioUrl = NormalizeOptional(dto.AudioUrl);
            lesson.Level = dto.Level;
            lesson.Order = dto.Order;
            lesson.IsPublished = dto.IsPublished;

            _lessonRepository.Update(lesson);
            await _lessonRepository.SaveChangesAsync();

            return MapToResponse(lesson);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            ValidateLessonId(id);

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
                AudioUrl = lesson.AudioUrl,
                Level = lesson.Level,
                Order = lesson.Order,
                IsPublished = lesson.IsPublished
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void ValidateLessonId(int id)
        {
            if (id <= 0)
                throw new ValidationException("LessonId must be greater than 0.");
        }

        private static void ValidateCreateDto(CreateLessonDto dto)
        {
            if (dto is null)
                throw new ValidationException("Lesson payload is required.");

            ValidateLessonFields(dto.Title, dto.Description, dto.AudioUrl, dto.Order, dto.Level);
        }

        private static void ValidateUpdateDto(UpdateLessonDto dto)
        {
            if (dto is null)
                throw new ValidationException("Lesson payload is required.");

            ValidateLessonFields(dto.Title, dto.Description, dto.AudioUrl, dto.Order, dto.Level);
        }

        private static void ValidateLessonFields(string title, string? description, string? audioUrl, int order, LanguageLevel level)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ValidationException("Lesson title is required.");

            if (title.Trim().Length > 200)
                throw new ValidationException("Lesson title must not exceed 200 characters.");

            if (description is not null && description.Trim().Length > 1000)
                throw new ValidationException("Lesson description must not exceed 1000 characters.");

            if (audioUrl is not null && audioUrl.Trim().Length > 2000)
                throw new ValidationException("Lesson audio URL must not exceed 2000 characters.");

            if (order <= 0)
                throw new ValidationException("Lesson order must be greater than 0.");

            if (!Enum.IsDefined(typeof(LanguageLevel), level))
                throw new ValidationException("Invalid lesson level.");
        }
    }
}
