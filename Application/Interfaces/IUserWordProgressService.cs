using System;
using System.Collections.Generic;
using System.Text;

using Application.DTOs.Progress;

namespace Application.Interfaces
{
    public interface IUserWordProgressService
    {
        Task UpdateAsync(int userId, int wordId, bool isCorrect);
        Task<List<UserWordProgressResponseDto>> GetByUserAsync(int userId);
        Task<UserWordProgressResponseDto?> GetByUserAndWordAsync(int userId, int wordId);
    }
}
