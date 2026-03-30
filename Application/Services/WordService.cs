using Application.DTOs.Words;
using Application.Interfaces;
using Domain.Entities;

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

            return words.Select(x => new WordResponseDto
            {
                Id = x.Id,
                KazakhText = x.KazakhText,
                RussianTranslation = x.RussianTranslation,
                Pronunciation = x.Pronunciation,
                Example = x.Example,
                AudioUrl = x.AudioUrl,
                ImageUrl = x.ImageUrl,
                Level = x.Level,
                IsActive = x.IsActive,
                CategoryId = x.CategoryId
            }).ToList();
        }

        public async Task<WordResponseDto?> GetByIdAsync(int id)
        {
            var word = await _wordRepository.GetByIdAsync(id);

            if (word is null)
                return null;

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

        public async Task<int> CreateAsync(CreateWordDto dto)
        {
            var word = new Word
            {
                KazakhText = dto.KazakhText,
                RussianTranslation = dto.RussianTranslation,
                Pronunciation = dto.Pronunciation,
                Example = dto.Example,
                AudioUrl = dto.AudioUrl,
                ImageUrl = dto.ImageUrl,
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
            var word = await _wordRepository.GetByIdAsync(id);

            if (word is null)
                return false;

            word.KazakhText = dto.KazakhText;
            word.RussianTranslation = dto.RussianTranslation;
            word.Pronunciation = dto.Pronunciation;
            word.Example = dto.Example;
            word.AudioUrl = dto.AudioUrl;
            word.ImageUrl = dto.ImageUrl;
            word.Level = dto.Level;
            word.IsActive = dto.IsActive;
            word.CategoryId = dto.CategoryId;

            _wordRepository.Update(word);
            await _wordRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var word = await _wordRepository.GetByIdAsync(id);

            if (word is null)
                return false;

            _wordRepository.Delete(word);
            await _wordRepository.SaveChangesAsync();

            return true;
        }
    }
}