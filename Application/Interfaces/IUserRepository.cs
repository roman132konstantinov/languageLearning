using Application.Common.Pagination;
using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserRepository
    {
        Task<PagedResult<User>> GetAllAsync(UserQueryDto query);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
        Task SaveChangesAsync();
    }
}
