using Application.Common.Exceptions;
using Application.DTOs.Words;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class WordService : IWordService
    {
        private readonly IWordRepository _wordRepository;

        public WordService(IWordRepository wordRepository)
        {
            _wordRepository = wordRepository;
        }

        public async Task<List<WordResponseDto>> GetAllAsync()
        {
            var words = await _wordRepository.GetAllAsync();
            return words.Select(MapToResponse).ToList();
        }

        public async Task<WordResponseDto?> GetByIdAsync(int id)
        {
            ValidateWordId(id);

            var word = await _wordRepository.GetByIdAsync(id);
            return word is null ? null : MapToResponse(word);
        }

        public async Task<int> CreateAsync(CreateWordDto dto)
        {
            ValidateCreateDto(dto);

            if (!await _wordRepository.CategoryExistsAsync(dto.CategoryId))
                throw new NotFoundException("Category not found.");

            var word = new Word
            {
                KazakhText = dto.KazakhText.Trim(),
                RussianTranslation = dto.RussianTranslation.Trim(),
                Pronunciation = NormalizeOptional(dto.Pronunciation),
                Example = NormalizeOptional(dto.Example),
                AudioUrl = NormalizeOptional(dto.AudioUrl),
                ImageUrl = NormalizeOptional(dto.ImageUrl),
                Level = dto.Level,
                IsActive = dto.IsActive,
                CategoryId = dto.CategoryId
            };

            await _wordRepository.AddAsync(word);
            await _wordRepository.SaveChangesAsync();

            return word.Id;
        }

        public async Task<bool> UpdateAsync(int id, UpdateWordDto dto)
        {
            ValidateWordId(id);
            ValidateUpdateDto(dto);

            var word = await _wordRepository.GetByIdAsync(id);
            if (word is null)
                return false;

            if (!await _wordRepository.CategoryExistsAsync(dto.CategoryId))
                throw new NotFoundException("Category not found.");

            word.KazakhText = dto.KazakhText.Trim();
            word.RussianTranslation = dto.RussianTranslation.Trim();
            word.Pronunciation = NormalizeOptional(dto.Pronunciation);
            word.Example = NormalizeOptional(dto.Example);
            word.AudioUrl = NormalizeOptional(dto.AudioUrl);
            word.ImageUrl = NormalizeOptional(dto.ImageUrl);
            word.Level = dto.Level;
            word.IsActive = dto.IsActive;
            word.CategoryId = dto.CategoryId;

            _wordRepository.Update(word);
            await _wordRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            ValidateWordId(id);

            var word = await _wordRepository.GetByIdAsync(id);
            if (word is null)
                return false;

            _wordRepository.Delete(word);
            await _wordRepository.SaveChangesAsync();

            return true;
        }

        private static WordResponseDto MapToResponse(Word word)
        {
            return new WordResponseDto
            {
                Id = word.Id,
                KazakhText = word.KazakhText,
                RussianTranslation = word.RussianTranslation,
                Pronunciation = word.Pronunciation,
                Example = word.Example,
                AudioUrl = word.AudioUrl,
                ImageUrl = word.ImageUrl,
                Level = word.Level,
                IsActive = word.IsActive,
                CategoryId = word.CategoryId
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void ValidateWordId(int id)
        {
            if (id <= 0)
                throw new ValidationException("WordId must be greater than 0.");
        }

        private static void ValidateCreateDto(CreateWordDto dto)
        {
            if (dto is null)
                throw new ValidationException("Word payload is required.");

            ValidateWordFields(dto.KazakhText, dto.RussianTranslation, dto.Pronunciation, dto.Example, dto.AudioUrl, dto.ImageUrl, dto.CategoryId, dto.Level);
        }

        private static void ValidateUpdateDto(UpdateWordDto dto)
        {
            if (dto is null)
                throw new ValidationException("Word payload is required.");

            ValidateWordFields(dto.KazakhText, dto.RussianTranslation, dto.Pronunciation, dto.Example, dto.AudioUrl, dto.ImageUrl, dto.CategoryId, dto.Level);
        }

        private static void ValidateWordFields(
            string kazakhText,
            string russianTranslation,
            string? pronunciation,
            string? example,
            string? audioUrl,
            string? imageUrl,
            int categoryId,
            LanguageLevel level)
        {
            if (string.IsNullOrWhiteSpace(kazakhText))
                throw new ValidationException("Kazakh text is required.");

            if (kazakhText.Trim().Length > 200)
                throw new ValidationException("Kazakh text must not exceed 200 characters.");

            if (string.IsNullOrWhiteSpace(russianTranslation))
                throw new ValidationException("Russian translation is required.");

            if (russianTranslation.Trim().Length > 200)
                throw new ValidationException("Russian translation must not exceed 200 characters.");

            if (pronunciation is not null && pronunciation.Trim().Length > 200)
                throw new ValidationException("Pronunciation must not exceed 200 characters.");

            if (example is not null && example.Trim().Length > 500)
                throw new ValidationException("Example must not exceed 500 characters.");

            if (audioUrl is not null && audioUrl.Trim().Length > 500)
                throw new ValidationException("AudioUrl must not exceed 500 characters.");

            if (imageUrl is not null && imageUrl.Trim().Length > 500)
                throw new ValidationException("ImageUrl must not exceed 500 characters.");

            if (categoryId <= 0)
                throw new ValidationException("CategoryId must be greater than 0.");

            if (!Enum.IsDefined(typeof(LanguageLevel), level))
                throw new ValidationException("Invalid language level.");
        }
    }
}
