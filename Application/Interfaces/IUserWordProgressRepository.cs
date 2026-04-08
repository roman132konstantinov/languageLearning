using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IUserWordProgressRepository
    {
        Task<bool> UserExistsAsync(int userId);
        Task<UserWordProgress?> GetByUserAndWordAsync(int userId, int wordId);
        Task<List<UserWordProgress>> GetByUserAsync(int userId);
        Task AddAsync(UserWordProgress progress);
        Task SaveChangesAsync();
    }
}
