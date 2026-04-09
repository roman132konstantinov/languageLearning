using Application.Common.Pagination;
using Application.DTOs.Lessons;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class LessonRepository : ILessonRepository
    {
        private readonly LanguageLearningDbContext _db;

        public LessonRepository(LanguageLearningDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<Lesson>> GetAllAsync(LessonQueryDto query)
        {
            var lessonsQuery = _db.Lessons.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                lessonsQuery = lessonsQuery.Where(x =>
                    x.Title.Contains(search) ||
                    (x.Description != null && x.Description.Contains(search)));
            }

            lessonsQuery = ApplySorting(lessonsQuery, query.SortBy, PagedQueryNormalizer.IsDescending(query.SortOrder));

            var totalCount = await lessonsQuery.CountAsync();
            var items = await lessonsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Lesson>(items, query.Page, query.PageSize, totalCount);
        }

        public async Task<Lesson?> GetByIdAsync(int id)
        {
            return await _db.Lessons
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsWithOrderAsync(int order, int? excludeLessonId = null)
        {
            return await _db.Lessons
                .AnyAsync(x => x.Order == order
                    && (!excludeLessonId.HasValue || x.Id != excludeLessonId.Value));
        }

        public async Task AddAsync(Lesson lesson)
        {
            await _db.Lessons.AddAsync(lesson);
        }

        public void Update(Lesson lesson)
        {
            _db.Lessons.Update(lesson);
        }

        public void Delete(Lesson lesson)
        {
            _db.Lessons.Remove(lesson);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        private static IQueryable<Lesson> ApplySorting(IQueryable<Lesson> query, string? sortBy, bool descending)
        {
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "id" => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
                "title" => descending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "level" => descending ? query.OrderByDescending(x => x.Level) : query.OrderBy(x => x.Level),
                "ispublished" => descending ? query.OrderByDescending(x => x.IsPublished) : query.OrderBy(x => x.IsPublished),
                _ => descending ? query.OrderByDescending(x => x.Order) : query.OrderBy(x => x.Order)
            };
        }
    }
}
