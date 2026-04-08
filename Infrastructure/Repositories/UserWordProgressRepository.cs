using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserWordProgressRepository : IUserWordProgressRepository
    {
        private readonly LanguageLearningDbContext _context;

        public UserWordProgressRepository(LanguageLearningDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            return await _context.Users.AnyAsync(x => x.Id == userId);
        }

        public async Task<UserWordProgress?> GetByUserAndWordAsync(int userId, int wordId)
        {
            return await _context.UserWordProgresses
                .FirstOrDefaultAsync(x => x.UserId == userId && x.WordId == wordId);
        }

        public async Task AddAsync(UserWordProgress progress)
        {
            await _context.UserWordProgresses.AddAsync(progress);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
