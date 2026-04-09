using Application.Common.Pagination;
using Application.DTOs.Category;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly LanguageLearningDbContext _db;

        public CategoryRepository(LanguageLearningDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<Category>> GetAllAsync(CategoryQueryDto query)
        {
            var categoriesQuery = _db.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                categoriesQuery = categoriesQuery.Where(x =>
                    x.Name.Contains(search) ||
                    (x.Description != null && x.Description.Contains(search)));
            }

            categoriesQuery = ApplySorting(categoriesQuery, query.SortBy, PagedQueryNormalizer.IsDescending(query.SortOrder));

            var totalCount = await categoriesQuery.CountAsync();
            var items = await categoriesQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Category>(items, query.Page, query.PageSize, totalCount);
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeCategoryId = null)
        {
            var normalized = name.Trim().ToLower();

            return await _db.Categories
                .AnyAsync(x => x.Name.ToLower() == normalized
                    && (!excludeCategoryId.HasValue || x.Id != excludeCategoryId.Value));
        }

        public async Task<bool> HasWordsAsync(int id)
        {
            return await _db.Words.AnyAsync(x => x.CategoryId == id);
        }

        public async Task AddAsync(Category category)
        {
            await _db.Categories.AddAsync(category);
        }

        public void Update(Category category)
        {
            _db.Categories.Update(category);
        }

        public void Delete(Category category)
        {
            _db.Categories.Remove(category);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        private static IQueryable<Category> ApplySorting(IQueryable<Category> query, string? sortBy, bool descending)
        {
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "id" => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
                "description" => descending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
                _ => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
            };
        }
    }
}
