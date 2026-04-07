using Application.DTOs.Lessons;
using Application.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

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

            return lessons.Select(x => new LessonResponseDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Level = x.Level,
                Order = x.Order,
                IsPublished = x.IsPublished
            }).ToList();
        }

        public async Task<LessonResponseDto?> GetByIdAsync(int id)
        {
            var lesson = await _lessonRepository.GetByIdAsync(id);

            if (lesson is null)
                return null;

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

        public async Task<LessonResponseDto> CreateAsync(CreateLessonDto dto)
        {
            var lesson = new Lesson
            {
                Title = dto.Title,
                Description = dto.Description,
                Level = dto.Level,
                Order = dto.Order,
                IsPublished = dto.IsPublished
            };

            await _lessonRepository.AddAsync(lesson);
            await _lessonRepository.SaveChangesAsync();

            // сразу возвращаем DTO
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

        public async Task<bool> UpdateAsync(int id, UpdateLessonDto dto)
        {
            var lesson = await _lessonRepository.GetByIdAsync(id);

            if (lesson is null)
                return false;

            lesson.Title = dto.Title;
            lesson.Description = dto.Description;
            lesson.Level = dto.Level;
            lesson.Order = dto.Order;
            lesson.IsPublished = dto.IsPublished;

            _lessonRepository.Update(lesson);
            await _lessonRepository.SaveChangesAsync();

            return true;
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
    }
}
