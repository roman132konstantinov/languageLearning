using Application.DTOs.LessonWords;
using Application.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class LessonWordService : ILessonWordService
    {
        private readonly ILessonWordRepository _lessonWordRepository;

        public LessonWordService(ILessonWordRepository lessonWordRepository)
        {
            _lessonWordRepository = lessonWordRepository;
        }

        public async Task<List<LessonWordResponseDto>> GetLessonWordsAsync(int lessonId)
        {
            ValidateLessonId(lessonId);

            var lessonExists = await _lessonWordRepository.LessonExistsAsync(lessonId);
            if (!lessonExists)
                throw new ArgumentException("Lesson not found.");

            var lessonWords = await _lessonWordRepository.GetLessonWordsAsync(lessonId);

            return lessonWords
                .Select(MapToResponse)
                .ToList();
        }

        public async Task AddWordToLessonAsync(int lessonId, AddWordToLessonDto dto)
        {
            ValidateLessonId(lessonId);
            ValidateAddDto(dto);

            var lessonExists = await _lessonWordRepository.LessonExistsAsync(lessonId);
            if (!lessonExists)
                throw new ArgumentException("Lesson not found.");

            var wordExists = await _lessonWordRepository.WordExistsAsync(dto.WordId);
            if (!wordExists)
                throw new ArgumentException("Word not found.");

            var alreadyExists = await _lessonWordRepository.ExistsAsync(lessonId, dto.WordId);
            if (alreadyExists)
                throw new ArgumentException("This word is already added to the lesson.");

            var lessonWord = new LessonWord
            {
                LessonId = lessonId,
                WordId = dto.WordId
            };

            await _lessonWordRepository.AddAsync(lessonWord);
            await _lessonWordRepository.SaveChangesAsync();
        }

        public async Task<bool> RemoveWordFromLessonAsync(int lessonId, int wordId)
        {
            ValidateLessonId(lessonId);
            ValidateWordId(wordId);

            var lessonWord = await _lessonWordRepository.GetByIdsAsync(lessonId, wordId);
            if (lessonWord is null)
                return false;

            _lessonWordRepository.Delete(lessonWord);
            await _lessonWordRepository.SaveChangesAsync();

            return true;
        }

        private static LessonWordResponseDto MapToResponse(LessonWord lessonWord)
        {
            return new LessonWordResponseDto
            {
                WordId = lessonWord.WordId,
                KazakhText = lessonWord.Word.KazakhText,
                RussianTranslation = lessonWord.Word.RussianTranslation,
                Pronunciation = lessonWord.Word.Pronunciation,
                Example = lessonWord.Word.Example,
                AudioUrl = lessonWord.Word.AudioUrl,
                ImageUrl = lessonWord.Word.ImageUrl,
                CategoryId = lessonWord.Word.CategoryId
            };
        }

        private static void ValidateLessonId(int lessonId)
        {
            if (lessonId <= 0)
                throw new ArgumentException("LessonId must be greater than 0.");
        }

        private static void ValidateWordId(int wordId)
        {
            if (wordId <= 0)
                throw new ArgumentException("WordId must be greater than 0.");
        }

        private static void ValidateAddDto(AddWordToLessonDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.WordId <= 0)
                throw new ArgumentException("WordId must be greater than 0.");
        }
    }
}
