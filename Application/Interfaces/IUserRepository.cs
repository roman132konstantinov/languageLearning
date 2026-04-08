using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
        Task SaveChangesAsync();
    }
}
