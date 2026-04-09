using Application.Common.Pagination;
using Application.DTOs.Words;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WordRepository : IWordRepository
    {
        private readonly LanguageLearningDbContext _db;

        public WordRepository(LanguageLearningDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<Word>> GetAllAsync(WordQueryDto query)
        {
            var wordsQuery = _db.Words.AsNoTracking().AsQueryable();

            if (query.Level.HasValue)
            {
                wordsQuery = wordsQuery.Where(x => x.Level == query.Level.Value);
            }

            if (query.CategoryId.HasValue)
            {
                wordsQuery = wordsQuery.Where(x => x.CategoryId == query.CategoryId.Value);
            }

            if (query.IsActive.HasValue)
            {
                wordsQuery = wordsQuery.Where(x => x.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                wordsQuery = wordsQuery.Where(x =>
                    x.KazakhText.ToLower().Contains(search) ||
                    x.RussianTranslation.ToLower().Contains(search) ||
                    (x.Pronunciation != null && x.Pronunciation.ToLower().Contains(search)) ||
                    (x.Example != null && x.Example.ToLower().Contains(search)) ||
                    x.Category.Name.ToLower().Contains(search));
            }

            wordsQuery = ApplySorting(wordsQuery, query.SortBy, PagedQueryNormalizer.IsDescending(query.SortOrder));

            var totalCount = await wordsQuery.CountAsync();
            var items = await wordsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Word>(items, query.Page, query.PageSize, totalCount);
        }

        public async Task<Word?> GetByIdAsync(int id)
        {
            return await _db.Words
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> CategoryExistsAsync(int categoryId)
        {
            return await _db.Categories.AnyAsync(x => x.Id == categoryId);
        }

        public async Task AddAsync(Word word)
        {
            await _db.Words.AddAsync(word);
        }

        public void Update(Word word)
        {
            _db.Words.Update(word);
        }

        public void Delete(Word word)
        {
            _db.Words.Remove(word);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        private static IQueryable<Word> ApplySorting(IQueryable<Word> query, string? sortBy, bool descending)
        {
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "id" => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
                "russiantranslation" => descending ? query.OrderByDescending(x => x.RussianTranslation) : query.OrderBy(x => x.RussianTranslation),
                "level" => descending ? query.OrderByDescending(x => x.Level) : query.OrderBy(x => x.Level),
                "categoryid" => descending ? query.OrderByDescending(x => x.CategoryId) : query.OrderBy(x => x.CategoryId),
                "isactive" => descending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => descending ? query.OrderByDescending(x => x.KazakhText) : query.OrderBy(x => x.KazakhText)
            };
        }
    }
}
