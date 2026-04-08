using Application.Common.Exceptions;
using Application.DTOs.LessonWords;
using Application.Interfaces;
using Domain.Entities;

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

            if (!await _lessonWordRepository.LessonExistsAsync(lessonId))
                throw new NotFoundException("Lesson not found.");

            var lessonWords = await _lessonWordRepository.GetLessonWordsAsync(lessonId);
            return lessonWords.Select(MapToResponse).ToList();
        }

        public async Task AddWordToLessonAsync(int lessonId, AddWordToLessonDto dto)
        {
            ValidateLessonId(lessonId);
            ValidateAddDto(dto);

            if (!await _lessonWordRepository.LessonExistsAsync(lessonId))
                throw new NotFoundException("Lesson not found.");

            if (!await _lessonWordRepository.WordExistsAsync(dto.WordId))
                throw new NotFoundException("Word not found.");

            if (await _lessonWordRepository.ExistsAsync(lessonId, dto.WordId))
                throw new ConflictException("This word is already added to the lesson.");

            if (await _lessonWordRepository.ExistsWithOrderAsync(lessonId, dto.Order))
                throw new ConflictException("Lesson already contains a word with this order.");

            var lessonWord = new LessonWord
            {
                LessonId = lessonId,
                WordId = dto.WordId,
                Order = dto.Order
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
                Order = lessonWord.Order,
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
                throw new ValidationException("LessonId must be greater than 0.");
        }

        private static void ValidateWordId(int wordId)
        {
            if (wordId <= 0)
                throw new ValidationException("WordId must be greater than 0.");
        }

        private static void ValidateAddDto(AddWordToLessonDto dto)
        {
            if (dto is null)
                throw new ValidationException("Lesson word payload is required.");

            if (dto.WordId <= 0)
                throw new ValidationException("WordId must be greater than 0.");

            if (dto.Order <= 0)
                throw new ValidationException("Order must be greater than 0.");
        }
    }
}
