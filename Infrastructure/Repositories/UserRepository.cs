using Application.Common.Pagination;
using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LanguageLearningDbContext _context;

        public UserRepository(LanguageLearningDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<User>> GetAllAsync(UserQueryDto query)
        {
            var usersQuery = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                usersQuery = usersQuery.Where(x =>
                    x.Email.Contains(search) ||
                    x.UserName.Contains(search));
            }

            usersQuery = ApplySorting(usersQuery, query.SortBy, PagedQueryNormalizer.IsDescending(query.SortOrder));

            var totalCount = await usersQuery.CountAsync();
            var items = await usersQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<User>(items, query.Page, query.PageSize, totalCount);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }

        public void Delete(User user)
        {
            _context.Users.Remove(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, bool descending)
        {
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "id" => descending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
                "email" => descending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
                "role" => descending ? query.OrderByDescending(x => x.Role) : query.OrderBy(x => x.Role),
                "level" => descending ? query.OrderByDescending(x => x.Level) : query.OrderBy(x => x.Level),
                "createdat" => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                "lastloginat" => descending ? query.OrderByDescending(x => x.LastLoginAt) : query.OrderBy(x => x.LastLoginAt),
                "isactive" => descending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => descending ? query.OrderByDescending(x => x.UserName) : query.OrderBy(x => x.UserName)
            };
        }
    }
}
